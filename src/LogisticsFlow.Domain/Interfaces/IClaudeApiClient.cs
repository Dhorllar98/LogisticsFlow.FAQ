using LogisticsFlow.Domain.Entities;

namespace LogisticsFlow.Domain.Interfaces;

/// <summary>
/// Contract for sending a grounded prompt and conversation history to
/// Claude. Returns the raw text response — Application is responsible
/// for parsing and validating it as structured JSON.
/// </summary>
public interface IClaudeApiClient
{
    Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);
}