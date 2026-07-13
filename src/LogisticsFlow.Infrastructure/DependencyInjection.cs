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

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LogisticsFlowDb")));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IRateAgreementRepository, RateAgreementRepository>();
        services.AddScoped<IRedactionService, PresidioRedactionService>();
        services.AddScoped<ITrackingRepository, TrackingRepository>();
        services.AddScoped<ILaneHistoryRepository, LaneHistoryRepository>();

        return services;
    }
}