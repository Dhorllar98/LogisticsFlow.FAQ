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
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddMemoryCache();
        services.AddSingleton<IFAQCacheService, FAQCacheService>();
        services.AddSingleton<IFAQRepository, JsonFAQRepository>();

        services.AddHttpClient<IClaudeApiClient, ClaudeApiClient>()
            .AddStandardResilienceHandler(options =>
            {
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