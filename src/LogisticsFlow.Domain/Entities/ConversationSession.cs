namespace LogisticsFlow.Domain.Entities;

/// <summary>
/// Tracks an in-progress conversation. Enforces the "last 4-6 turns"
/// context window rule directly at the entity level, so no caller can
/// accidentally let history grow unbounded.
/// </summary>
public class ConversationSession
{
    private const int MaxHistoryTurns = 6;

    public Guid SessionId { get; set; } = Guid.NewGuid();
    public List<ChatMessage> Messages { get; private set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);

        if (Messages.Count > MaxHistoryTurns)
        {
            Messages.RemoveAt(0);
        }
    }
}