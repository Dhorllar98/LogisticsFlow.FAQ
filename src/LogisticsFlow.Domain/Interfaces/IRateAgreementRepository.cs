using LogisticsFlow.Domain.Entities;

namespace LogisticsFlow.Domain.Interfaces;

public interface IRateAgreementRepository
{
    /// <summary>
    /// Returns the currently effective rate agreement for the given client,
    /// or null if none exists (caller decides whether that's a 404).
    /// </summary>
    Task<RateAgreement?> GetCurrentForClientAsync(Guid clientId, CancellationToken ct = default);
}
