using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.AI;
using LogisticsFlow.Infrastructure.Cache;
using LogisticsFlow.Infrastructure.Persistence;
using LogisticsFlow.Infrastructure.Redaction;
using LogisticsFlow.Infrastructure.Repositories;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace LogisticsFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Phase 1 - FAQ
        services.Configure<ClaudeApiSettings>(configuration.GetSection("ClaudeApi"));
        services.AddMemoryCache();
        services.AddSingleton<IFAQCacheService, FAQCacheService>();
        services.AddSingleton<IFAQRepository, JsonFAQRepository>();

        services.AddHttpClient<IClaudeApiClient, ClaudeApiClient>()
            .AddStandardResilienceHandler(options =>
            {
                // RESOLVED (was flagged): the default transient classifier
                // behind AddStandardResilienceHandler() covers 5xx/408 only.
                // CLAUDE.md's "fallback model on 429 or 5xx" requires 429 to
                // be retried too. We extend the default predicate rather
                // than replace it, so 5xx/408/timeout/network-failure
                // handling is unchanged - only 429 is newly added.
                var defaultPredicate = options.Retry.ShouldHandle;
                options.Retry.ShouldHandle = async args =>
                {
                    if (await defaultPredicate(args))
                        return true;

                    return args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests;
                };
            });

        // Phase 2 - Quotation
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("LogisticsFlowDb")));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IRateAgreementRepository, RateAgreementRepository>();
        services.AddScoped<IRedactionService, PresidioRedactionService>();

        return services;
    }
}
