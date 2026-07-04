namespace LogisticsFlow.Domain.Exceptions;

/// <summary>
/// Thrown when the AI's composed status summary fails basic sanity
/// checks (empty, whitespace-only). Maps to 422 via the generic
/// BusinessRuleException case in GlobalExceptionMiddleware — this is
/// system/infra failure territory per CLAUDE.md, not a business
/// escalation, since Tracking has no FAQ-style "confidence score" or
/// "escalate to human" concept. An unusable AI response here means the
/// request genuinely failed, not that a lower-confidence answer was given.
/// </summary>
public class TrackingResponseInvalidException : BusinessRuleException
{
    public TrackingResponseInvalidException(string message) : base(message) { }
}