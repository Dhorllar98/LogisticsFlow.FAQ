using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsFlow.Infrastructure.Repositories;

public class TrackingRepository : ITrackingRepository
{
    private readonly AppDbContext _context;

    public TrackingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Shipment?> GetByTrackingNumberForAccountAsync(
        string trackingNumber, string accountId, CancellationToken cancellationToken)
    {
        return await _context.Shipments
            .AsNoTracking()
            .Include(s => s.Events)
            .Join(
                _context.Clients.AsNoTracking(),
                shipment => shipment.ClientId,
                client => client.Id,
                (shipment, client) => new { shipment, client.AccountId })
            .Where(x => x.shipment.TrackingNumber == trackingNumber && x.AccountId == accountId)
            .Select(x => x.shipment)
            .FirstOrDefaultAsync(cancellationToken);
    }
}