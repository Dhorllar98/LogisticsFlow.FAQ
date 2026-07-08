namespace LogisticsFlow.Domain.Interfaces;

using LogisticsFlow.Domain.Entities;

public interface ITrackingRepository
{
    /// <summary>
    /// Resolves a shipment by tracking number, scoped to the given
    /// account. Returns null if the tracking number doesn't exist OR
    /// exists under a different account — the two cases are
    /// indistinguishable by design, enforced at the query level, so no
    /// caller can accidentally leak cross-account existence through a
    /// timing difference or separate code path.
    /// </summary>
    Task<Shipment?> GetByTrackingNumberForAccountAsync(
        string trackingNumber, string accountId, CancellationToken cancellationToken);
}