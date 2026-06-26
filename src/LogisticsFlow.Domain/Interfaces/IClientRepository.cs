using LogisticsFlow.Domain.Entities;

namespace LogisticsFlow.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByAccountIdAsync(string accountId, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
