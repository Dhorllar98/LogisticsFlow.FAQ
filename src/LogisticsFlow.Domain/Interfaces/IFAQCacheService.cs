namespace LogisticsFlow.Domain.Interfaces;

public interface IFAQCacheService
{
    Task<string?> GetAsync(string normalizedQuery, CancellationToken cancellationToken = default);
    Task SetAsync(string normalizedQuery, string rawResponse, CancellationToken cancellationToken = default);
}