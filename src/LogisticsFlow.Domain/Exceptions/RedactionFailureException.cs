namespace LogisticsFlow.Domain.Exceptions;

/// <summary>
/// Thrown when a redaction map fails to restore original values into a
/// Claude response (e.g. token mismatch, map shorter than expected).
/// Per CLAUDE.md: restore failure must be a hard 422, never a partial
/// or leaked response — explicit case in GlobalExceptionMiddleware
/// enforces this (it would already fall into the generic
/// BusinessRuleException -> 422 case, but is made explicit per the
/// security checklist's "deliberate status codes, not generic catch-all"
/// rule).
/// </summary>
public class RedactionFailureException : BusinessRuleException
{
    public RedactionFailureException(string message) : base(message) { }

    public static RedactionFailureException RestoreMismatch(string token) =>
        new($"Failed to restore token '{token}' — no matching entry in redaction map.");
}
