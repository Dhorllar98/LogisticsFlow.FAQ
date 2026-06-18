using System.Text.Json;
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

        // Only standalone questions (no prior history) are cacheable —
        // a follow-up's correct answer depends on context, so it is
        // never served from cache.
        var isCacheable = request.History is null || request.History.Count == 0;
        var normalizedQuery = request.Query.Trim().ToLowerInvariant();

        string? rawResponse = isCacheable
            ? await _cacheService.GetAsync(normalizedQuery, cancellationToken)
            : null;

        if (rawResponse is null)
        {
            var entries = await _faqRepository.GetAllAsync();
            var knowledgeBaseBlock = string.Join(
                Environment.NewLine,
                entries.Select(e => $"[{e.Id}] ({e.Category}) Q: {e.Question} A: {e.Answer}"));

            var systemPrompt = SystemPrompts.FaqAssistantV1.Replace("{{KNOWLEDGE_BASE}}", knowledgeBaseBlock);

            rawResponse = await _claudeApiClient.SendMessageAsync(systemPrompt, session.Messages, cancellationToken);

            if (isCacheable)
                await _cacheService.SetAsync(normalizedQuery, rawResponse, cancellationToken);
        }

        // Parsing, validation, and escalation logic run identically
        // regardless of whether rawResponse came from cache or Claude.
        RawAiResponse parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RawAiResponse>(
                rawResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
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

        return response;
    }

    private sealed record RawAiResponse(string Answer, string Category, double ConfidenceScore, List<string>? GroundingSources);
}