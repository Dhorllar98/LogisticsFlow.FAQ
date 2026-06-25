using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogisticsFlow.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;

    public ClientRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Client?> GetByAccountIdAsync(string accountId, CancellationToken ct = default) =>
        _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.AccountId == accountId, ct);

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
}
