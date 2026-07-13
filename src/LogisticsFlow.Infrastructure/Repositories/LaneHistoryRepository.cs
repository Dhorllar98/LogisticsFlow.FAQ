using LogisticsFlow.Domain.Constants;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Domain.ValueObjects;
using LogisticsFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsFlow.Infrastructure.Repositories;

public class LaneHistoryRepository : ILaneHistoryRepository
{
    // Below this sample size, an "average" risks being effectively one
    // or two shipments wearing an aggregate label - see CLAUDE.md
    // Phase 3.5 Tier declaration for the privacy reasoning.
    private const int MinimumSampleSize = 5;

    private readonly AppDbContext _context;

    public LaneHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LaneHistoryResult?> GetLaneStatsAsync(
        string carrier,
        ShipmentMode mode,
        string originRegion,
        string destinationRegion,
        CancellationToken cancellationToken)
    {
        // Only completed journeys contribute to the average - an in-transit
        // shipment's partial elapsed time is not a valid transit-duration
        // sample and would skew the average downward if included.
        var samples = await _context.Shipments
            .AsNoTracking()
            .Where(s => s.Carrier == carrier
                && s.Mode == mode
                && s.OriginRegion == originRegion
                && s.DestinationRegion == destinationRegion
                && s.Events.Any(e => e.MilestoneType == MilestoneTypes.Delivered))
            .Select(s => new
            {
                s.CreatedAtUtc,
                DeliveredAtUtc = s.Events
                    .Where(e => e.MilestoneType == MilestoneTypes.Delivered)
                    .Min(e => e.TimestampUtc)
            })
            .ToListAsync(cancellationToken);

        if (samples.Count < MinimumSampleSize)
        {
            return null;
        }

        var averageDays = samples
            .Select(s => (s.DeliveredAtUtc - s.CreatedAtUtc).TotalDays)
            .Average();

        return new LaneHistoryResult(averageDays, samples.Count);
    }
}