using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.ValueObjects;

namespace LogisticsFlow.Domain.Interfaces;

public interface ILaneHistoryRepository
{
    /// <summary>
    /// Returns pooled, depersonalized transit-time statistics for the
    /// given lane, computed only from delivered shipments, or null if
    /// fewer than the minimum sample size exists. The minimum-sample-size
    /// floor is enforced by the implementation, not the caller - see
    /// LaneHistoryRepository.
    /// </summary>
    Task<LaneHistoryResult?> GetLaneStatsAsync(
        string carrier,
        ShipmentMode mode,
        string originRegion,
        string destinationRegion,
        CancellationToken cancellationToken);
}