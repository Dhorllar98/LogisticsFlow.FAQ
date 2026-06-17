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
    private readonly IValidator<FAQResponseDto> _responseValidator;

    public FAQService(
        IFAQRepository faqRepository,
        IClaudeApiClient claudeApiClient,
        IValidator<FAQResponseDto> responseValidator)
    {
        _faqRepository = faqRepository;
        _claudeApiClient = claudeApiClient;
        _responseValidator = responseValidator;
    }

    public async Task<FAQResponseDto> AskAsync(FAQRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Build the capped conversation history (max 6 turns), reusing
        //    the Domain entity's own enforcement logic rather than
        //    duplicating the cap here.
        var session = new ConversationSession();
        if (request.History is not null)
        {
            foreach (var historyMessage in request.History)
            {
                session.AddMessage(historyMessage);
            }
        }
        session.AddMessage(new ChatMessage { Role = ChatRole.User, Content = request.Query });

        // 2. Load the knowledge base and build the grounded system prompt.
        var entries = await _faqRepository.GetAllAsync();
        var knowledgeBaseBlock = string.Join(
            Environment.NewLine,
            entries.Select(e => $"[{e.Id}] ({e.Category}) Q: {e.Question} A: {e.Answer}"));

        var systemPrompt = SystemPrompts.FaqAssistantV1
            .Replace("{{KNOWLEDGE_BASE}}", knowledgeBaseBlock);

        // 3. Call Claude with the full message history (current query
        //    included as the final turn).
        var rawResponse = await _claudeApiClient.SendMessageAsync(
            systemPrompt,
            session.Messages,
            cancellationToken);

        // 4. Parse the structured output. Never trust raw LLM text in
        //    business logic without parsing and validating first.
        RawAiResponse parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RawAiResponse>(
                rawResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Deserialized response was null.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(
                $"AI returned malformed JSON output and could not be parsed: {ex.Message}");
        }

        if (!Enum.TryParse<LogisticCategory>(parsed.Category, ignoreCase: true, out var category))
        {
            throw new BusinessRuleException(
                $"AI returned an unrecognized logistics category: '{parsed.Category}'.");
        }

        var groundingSources = parsed.GroundingSources ?? new List<string>();

        // 5. Compute EscalationBoolean ourselves — never trust the model to
        //    self-report this. An empty grounding list always forces
        //    escalation regardless of the reported confidence score.
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

        // 6. Final safety net: validate the shaped response before it ever
        //    leaves the Application layer.
        var validationResult = await _responseValidator.ValidateAsync(response, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BusinessRuleException($"AI response failed validation: {errors}");
        }

        return response;
    }

    /// <summary>
    /// Intermediate shape matching the JSON schema requested in
    /// SystemPrompts.FaqAssistantV1. EscalationBoolean is deliberately
    /// absent — it is never requested from the model, only computed.
    /// </summary>
    private sealed record RawAiResponse(
        string Answer,
        string Category,
        double ConfidenceScore,
        List<string>? GroundingSources);
}