namespace LogisticsFlow.Domain.Exceptions;

/// <summary>
/// Thrown when the knowledge base itself cannot be established — e.g.
/// the JSON file fails to load or parse at startup. This is distinct
/// from a single query falling outside the knowledge boundary, which is
/// a normal business outcome handled via EscalationBoolean, not an
/// exception.
/// </summary>
public class KnowledgeBoundaryException : Exception
{
    public KnowledgeBoundaryException(string message) : base(message) { }
}