using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.AI;
using LogisticsFlow.Infrastructure.Cache;
using LogisticsFlow.Infrastructure.Repositories;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClaudeApiSettings>(configuration.GetSection("ClaudeApi"));
        services.AddMemoryCache();
        services.AddSingleton<IFAQCacheService, FAQCacheService>();
        services.AddSingleton<IFAQRepository, JsonFAQRepository>();
        services.AddHttpClient<IClaudeApiClient, ClaudeApiClient>()
            .AddStandardResilienceHandler();
        return services;
    }
}