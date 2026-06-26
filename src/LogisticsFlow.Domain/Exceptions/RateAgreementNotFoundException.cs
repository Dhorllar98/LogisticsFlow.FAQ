namespace LogisticsFlow.Domain.Exceptions;

/// <summary>
/// Thrown when no active RateAgreement exists for a given client/lane.
/// Maps to 404 at the Presentation boundary via an explicit case in
/// GlobalExceptionMiddleware — must be ordered BEFORE the generic
/// BusinessRuleException case there, since C# type-pattern switches
/// match base types against derived instances too.
/// </summary>
public class RateAgreementNotFoundException : BusinessRuleException
{
    public RateAgreementNotFoundException(string message) : base(message) { }

    public static RateAgreementNotFoundException ForClient(Guid clientId) =>
        new($"No active rate agreement found for client '{clientId}'.");
}
