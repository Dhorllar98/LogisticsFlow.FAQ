using LogisticsFlow.Domain.Entities;

namespace LogisticsFlow.Domain.Interfaces;

/// <summary>
/// Provider-agnostic contract for sending a grounded prompt and
/// conversation history to an LLM. Returns the raw text response —
/// Application is responsible for parsing and validating it as
/// structured JSON.
/// </summary>
public interface ILlmClient
{
    Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);
}
