namespace LogisticsFlow.Domain.ValueObjects;

/// <summary>
/// Aggregate-only lane transit statistics, pooled across all clients who
/// have shipped a given Carrier+Mode+OriginRegion+DestinationRegion
/// combination, using only delivered shipments. Never carries any single
/// shipment's identifiable data - see CLAUDE.md Phase 3.5 Tier
/// declaration. Repository implementations must never return this below
/// the minimum sample-size floor; callers treat a null result as
/// "insufficient historical data," not zero risk.
/// </summary>
public sealed record LaneHistoryResult(double AverageTransitDays, int SampleSize);