using LogisticsFlow.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace LogisticsFlow.Infrastructure.Cache;

public class FAQCacheService : IFAQCacheService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public FAQCacheService(IMemoryCache cache) => _cache = cache;

    public Task<string?> GetAsync(string normalizedQuery, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(BuildKey(normalizedQuery), out string? value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string normalizedQuery, string rawResponse, CancellationToken cancellationToken = default)
    {
        _cache.Set(BuildKey(normalizedQuery), rawResponse, CacheDuration);
        return Task.CompletedTask;
    }

    private static string BuildKey(string normalizedQuery) => $"faq:{normalizedQuery}";
}