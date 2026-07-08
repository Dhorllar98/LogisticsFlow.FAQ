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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind provider-specific settings — both always registered,
        // each client consumes only its own typed settings class.
        // ApiKey lives in environment variables / gitignored dev secrets only.
        services.Configure<ClaudeSettings>(configuration.GetSection("Providers:Claude"));
        services.Configure<OllamaSettings>(configuration.GetSection("Providers:Ollama"));
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddMemoryCache();
        services.AddSingleton<IFAQCacheService, FAQCacheService>();
        services.AddSingleton<IFAQRepository, JsonFAQRepository>();

        var activeProvider = configuration["ActiveProvider"] ?? "Claude";

        switch (activeProvider)
        {
            case "Ollama":
                services.AddHttpClient<ILlmClient, OllamaApiClient>();
                break;

            case "Claude":
            default:
                services.AddHttpClient<ILlmClient, ClaudeApiClient>()
                    .AddStandardResilienceHandler(options =>
                    {
                        // Extends the default transient classifier to include 429
                        // (rate limit) alongside the default 5xx/408/timeout coverage.
                        var defaultPredicate = options.Retry.ShouldHandle;
                        options.Retry.ShouldHandle = async args =>
                        {
                            if (await defaultPredicate(args))
                                return true;

                            return args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests;
                        };
                    });
                break;
        }

        // Npgsql (PostgreSQL) — provider-agnostic EF Core layer.
        // Connection string supplied entirely via environment variable in
        // production (ConnectionStrings__LogisticsFlowDb on Railway).
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LogisticsFlowDb")));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IRateAgreementRepository, RateAgreementRepository>();
        services.AddScoped<IRedactionService, PresidioRedactionService>();
        services.AddScoped<ITrackingRepository, TrackingRepository>();

        return services;
    }
}