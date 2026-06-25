using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.AI;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogisticsFlow.Integration.Tests;

/// <summary>
/// Wires the REAL ClaudeApiClient + AddStandardResilienceHandler() pipeline
/// against a fake primary HTTP handler, so these tests exercise the actual
/// production resilience config rather than a reimplemented copy of it.
/// </summary>
internal static class TestServiceProviderFactory
{
    public static IServiceProvider BuildWithFakeHandler(HttpMessageHandler fakeHandler)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.Configure<ClaudeApiSettings>(options =>
        {
            options.ApiKey = "test-key";
            options.Model = "claude-sonnet-4-6";
            options.BaseUrl = "https://fake.local/v1/messages";
            options.AnthropicVersion = "2023-06-01";
            options.MaxTokens = 1024;
        });

        // Identical to the real Infrastructure registration:
        // services.AddHttpClient<IClaudeApiClient, ClaudeApiClient>().AddStandardResilienceHandler();
        // — only the primary handler is swapped for the fake one below.
        services.AddHttpClient<IClaudeApiClient, ClaudeApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => fakeHandler)
            .AddStandardResilienceHandler();

        return services.BuildServiceProvider();
    }
}
