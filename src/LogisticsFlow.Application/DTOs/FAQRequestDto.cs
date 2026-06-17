using LogisticsFlow.Domain.Entities;

namespace LogisticsFlow.Application.DTOs;

/// <summary>
/// Incoming request to the FAQ endpoint. Conversation history is supplied
/// by the client (React maintains it in session state); the backend does
/// not persist sessions server-side in Phase 1.
/// </summary>
public class FAQRequestDto
{
    public string Query { get; set; } = string.Empty;
    public List<ChatMessage>? History { get; set; }
}