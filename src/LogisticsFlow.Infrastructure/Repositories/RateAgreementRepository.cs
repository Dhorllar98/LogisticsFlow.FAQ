using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsFlow.Infrastructure.Repositories;

public class RateAgreementRepository : IRateAgreementRepository
{
    private readonly AppDbContext _db;

    public RateAgreementRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RateAgreement?> GetCurrentForClientAsync(Guid clientId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _db.RateAgreements
            .AsNoTracking()
            .Where(r => r.ClientId == clientId && r.EffectiveFrom <= now)
            .Where(r => r.EffectiveTo == null || r.EffectiveTo >= now)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }
}
