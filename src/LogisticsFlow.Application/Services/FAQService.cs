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
using Microsoft.Extensions.Logging;

namespace LogisticsFlow.Application.Services;

public class FAQService : IFAQService
{
    private const double EscalationThreshold = 0.70;

    private const string RetryInstruction =
        "Your previous response was not valid JSON and could not be parsed. " +
        "Respond again with ONLY the JSON object matching the schema given " +
        "in the system prompt - no other text, no markdown formatting.";

    // RESOLVED (Finding A): Claude occasionally wraps JSON output in
    // markdown code fences (```json ... ```) despite the system prompt
    // explicitly saying not to. This strips them before parsing.
    private static readonly Regex MarkdownFencePattern =
        new(@"^\s*```(?:json)?\s*\n?(.*?)\n?\s*```\s*$", RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly IFAQRepository _faqRepository;
    private readonly ILlmClient _claudeApiClient;
    private readonly IFAQCacheService _cacheService;
    private readonly IValidator<FAQResponseDto> _responseValidator;
    private readonly ILogger<FAQService> _logger;

    public FAQService(
        IFAQRepository faqRepository,
        ILlmClient claudeApiClient,
        IFAQCacheService cacheService,
        IValidator<FAQResponseDto> responseValidator,
        ILogger<FAQService> logger)
    {
        _faqRepository = faqRepository;
        _claudeApiClient = claudeApiClient;
        _cacheService = cacheService;
        _responseValidator = responseValidator;
        _logger = logger;
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

        string? cachedResponse = isCacheable
            ? await _cacheService.GetAsync(normalizedQuery, cancellationToken)
            : null;

        var servedFromCache = cachedResponse is not null;

        string cleanedResponse;
        RawAiResponse parsed;

        if (cachedResponse is not null)
        {
            // Cache only ever stores responses that already parsed and
            // validated successfully (see the cache-write comment below),
            // so no retry path is needed here - a failure at this point
            // would indicate cache corruption, not a model output issue.
            cleanedResponse = StripMarkdownFence(cachedResponse);
            parsed = DeserializeOrThrow(cleanedResponse);
        }
        else
        {
            var entries = await _faqRepository.GetAllAsync();
            var knowledgeBaseBlock = string.Join(
                Environment.NewLine,
                entries.Select(e => $"[{e.Id}] ({e.Category}) Q: {e.Question} A: {e.Answer}"));

            var systemPrompt = SystemPrompts.FaqAssistantV1.Replace("{{KNOWLEDGE_BASE}}", knowledgeBaseBlock);

            var firstAttempt = await _claudeApiClient.SendMessageAsync(systemPrompt, session.Messages, cancellationToken);
            var firstCleaned = StripMarkdownFence(firstAttempt);

            // RESOLVED (Finding B, corrected approach): a prompt instruction
            // to "respond with ONLY JSON" is a request the model can still
            // ignore - it occasionally returned plain prose for out-of-scope
            // or edge-case queries. An earlier attempt at fixing this via
            // assistant message prefill was reverted: Claude Sonnet 5
            // rejects prefill outright ("This model does not support
            // assistant message prefill. The conversation must end with a
            // user message.", confirmed via a live 400 from the API, not
            // assumed). Retrying once with an explicit corrective user
            // turn is the approach that actually works with this model -
            // capped at exactly one retry, never a loop, consistent with
            // this project's Infinite Loop Guard principle even outside
            // a fully agentic flow.
            if (TryDeserialize(firstCleaned, out var firstParsed))
            {
                cleanedResponse = firstCleaned;
                parsed = firstParsed!;
            }
            else
            {
                _logger.LogWarning(
                    "FAQ response failed JSON parsing on first attempt. Retrying once with a corrective prompt. RawResponse={RawResponse}",
                    firstAttempt);

                var retryMessages = session.Messages
                    .Append(new ChatMessage { Role = ChatRole.Assistant, Content = firstAttempt })
                    .Append(new ChatMessage { Role = ChatRole.User, Content = RetryInstruction })
                    .ToList();

                var secondAttempt = await _claudeApiClient.SendMessageAsync(systemPrompt, retryMessages, cancellationToken);
                var secondCleaned = StripMarkdownFence(secondAttempt);

                if (TryDeserialize(secondCleaned, out var secondParsed))
                {
                    _logger.LogInformation("FAQ response parsed successfully on retry.");
                    cleanedResponse = secondCleaned;
                    parsed = secondParsed!;
                }
                else
                {
                    _logger.LogError(
                        "FAQ response failed JSON parsing on retry as well. Giving up. RawResponse={RawResponse}",
                        secondAttempt);
                    throw new BusinessRuleException(
                        "AI returned malformed JSON output and could not be parsed after one retry.");
                }
            }
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

        // RESOLVED (Finding A): cache write only happens AFTER successful
        // parse + validation, not before. Never re-cache something already
        // served from cache.
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

    private static RawAiResponse DeserializeOrThrow(string cleaned)
    {
        try
        {
            return JsonSerializer.Deserialize<RawAiResponse>(
                cleaned, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Deserialized response was null.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"AI returned malformed JSON output and could not be parsed: {ex.Message}");
        }
    }

    private static bool TryDeserialize(string cleaned, out RawAiResponse? parsed)
    {
        try
        {
            parsed = JsonSerializer.Deserialize<RawAiResponse>(
                cleaned, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed is not null;
        }
        catch (JsonException)
        {
            parsed = null;
            return false;
        }
    }

    private sealed record RawAiResponse(string Answer, string Category, double ConfidenceScore, List<string>? GroundingSources);
}