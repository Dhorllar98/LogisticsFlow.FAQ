namespace LogisticsFlow.Domain.Exceptions;

/// <summary>
/// Thrown when a request is well-formed and passes validation, but a
/// business rule rejects it — maps to HTTP 422 in the Presentation layer.
/// Example: the AI's structured JSON response fails schema validation
/// after parsing.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}