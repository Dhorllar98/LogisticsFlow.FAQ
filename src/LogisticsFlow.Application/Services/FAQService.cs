using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using LogisticsFlow.Application.Prompts;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;

namespace LogisticsFlow.Application.Services;

public class FAQService : IFAQService
{
    private const double EscalationThreshold = 0.70;

    // RESOLVED (Finding A, found in production via real Claude calls
    // tonight): Claude occasionally wraps JSON output in markdown code
    // fences (```json ... ```) despite the system prompt explicitly
    // saying not to. This strips them before parsing. Matches a leading
    // ```json or ``` and a trailing ```, tolerating surrounding whitespace.
    private static readonly Regex MarkdownFencePattern =
        new(@"^\s*```(?:json)?\s*\n?(.*?)\n?\s*```\s*$", RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly IFAQRepository _faqRepository;
    private readonly IClaudeApiClient _claudeApiClient;
    private readonly IFAQCacheService _cacheService;
    private readonly IValidator<FAQResponseDto> _responseValidator;

    public FAQService(
        IFAQRepository faqRepository,
        IClaudeApiClient claudeApiClient,
        IFAQCacheService cacheService,
        IValidator<FAQResponseDto> responseValidator)
    {
        _faqRepository = faqRepository;
        _claudeApiClient = claudeApiClient;
        _cacheService = cacheService;
        _responseValidator = responseValidator;
    }

    public async Task<FAQResponseDto> AskAsync(FAQRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = new ConversationSession();
        if (request.History is not null)
        {
            foreach (var historyMessage in request.History)
                session.AddMessage(historyMessage);
        }
        session.AddMessage(new ChatMessage { Role = ChatRole.User, Content = request.Query });

        var isCacheable = request.History is null || request.History.Count == 0;
        var normalizedQuery = request.Query.Trim().ToLowerInvariant();

        string? rawResponse = isCacheable
            ? await _cacheService.GetAsync(normalizedQuery, cancellationToken)
            : null;

        var servedFromCache = rawResponse is not null;

        if (rawResponse is null)
        {
            var entries = await _faqRepository.GetAllAsync();
            var knowledgeBaseBlock = string.Join(
                Environment.NewLine,
                entries.Select(e => $"[{e.Id}] ({e.Category}) Q: {e.Question} A: {e.Answer}"));

            var systemPrompt = SystemPrompts.FaqAssistantV1.Replace("{{KNOWLEDGE_BASE}}", knowledgeBaseBlock);

            rawResponse = await _claudeApiClient.SendMessageAsync(systemPrompt, session.Messages, cancellationToken);
        }

        // RESOLVED (Finding A): strip fences before parsing, regardless of
        // whether the response came from cache or a fresh Claude call -
        // a fenced response should never have been cached in the first
        // place under the old code path, but stripping here is also a
        // safe no-op against already-clean cached JSON.
        var cleanedResponse = StripMarkdownFence(rawResponse);

        RawAiResponse parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RawAiResponse>(
                cleanedResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Deserialized response was null.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"AI returned malformed JSON output and could not be parsed: {ex.Message}");
        }

        if (!Enum.TryParse<LogisticCategory>(parsed.Category, ignoreCase: true, out var category))
        {
            throw new BusinessRuleException($"AI returned an unrecognized logistics category: '{parsed.Category}'.");
        }

        var groundingSources = parsed.GroundingSources ?? new List<string>();
        var escalate = parsed.ConfidenceScore < EscalationThreshold || groundingSources.Count == 0;

        var response = new FAQResponseDto
        {
            Answer = parsed.Answer,
            Category = category,
            ConfidenceScore = parsed.ConfidenceScore,
            GroundingSources = groundingSources,
            EscalationBoolean = escalate,
            SessionId = session.SessionId
        };

        var validationResult = await _responseValidator.ValidateAsync(response, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BusinessRuleException($"AI response failed validation: {errors}");
        }

        // RESOLVED (Finding A): cache write moved to AFTER successful
        // parse + validation, not before. The old code cached rawResponse
        // immediately after the Claude call, meaning a malformed or
        // fence-wrapped response got cached and then repeatedly served
        // and re-failed on every subsequent identical query until the
        // 24-hour TTL expired. Only ever cache a response we know is
        // valid. Never re-cache something already served from cache.
        if (isCacheable && !servedFromCache)
        {
            await _cacheService.SetAsync(normalizedQuery, cleanedResponse, cancellationToken);
        }

        return response;
    }

    private static string StripMarkdownFence(string raw)
    {
        var match = MarkdownFencePattern.Match(raw);
        return match.Success ? match.Groups[1].Value : raw;
    }

    private sealed record RawAiResponse(string Answer, string Category, double ConfidenceScore, List<string>? GroundingSources);
}