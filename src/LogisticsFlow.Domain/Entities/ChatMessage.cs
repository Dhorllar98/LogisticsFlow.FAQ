namespace LogisticsFlow.Domain.Entities;

/// <summary>
/// Who authored a given turn in a conversation.
/// </summary>
public enum ChatRole
{
    User,
    Assistant
}

/// <summary>
/// A single turn in a conversation. Infrastructure is responsible for
/// translating ChatRole to the string format the Claude API expects
/// ("user" / "assistant") — that mapping does not belong here.
/// </summary>
public class ChatMessage
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}