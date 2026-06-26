using System.Net;
using System.Net.Http.Json;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

public class RateLimiterLoadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FakeClaudeApiClient : IClaudeApiClient
    {
        public Task<string> SendMessageAsync(
            string systemPrompt, IReadOnlyList<ChatMessage> conversationHistory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                """{"answer":"Fake answer for load testing.","category":"General","confidenceScore":0.9,"groundingSources":["L-001"]}""");
        }
    }

    private readonly WebApplicationFactory<Program> _factory;

    public RateLimiterLoadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IClaudeApiClient>();
                services.AddSingleton<IClaudeApiClient, FakeClaudeApiClient>();
            });
        });
    }

    private HttpClient CreateClientWithIp(string simulatedIp)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", simulatedIp);
        return client;
    }

    /// <summary>
    /// RESOLVED: /api/quotation/quote now requires a Bearer token
    /// (JWT auth added after this test was first written). Fetches a
    /// dev-only token once per client and attaches it to every
    /// Quotation request below, so the rate limiter - not auth - is
    /// what's actually being exercised.
    /// </summary>
    private static async Task<HttpClient> WithDevTokenAsync(HttpClient client)
    {
        var tokenResponse = await client.PostAsJsonAsync(
            "/api/quotation/token", new { accountId = "ACC-DEMO-001", secret = "demo-secret-001" });
        var token = (await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>())!.Token;
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private sealed record DevTokenResponse(string Token);

    [Fact]
    public async Task FaqEndpoint_BurstAboveLimit_Returns429WithRetryAfter()
    {
        var client = CreateClientWithIp("203.0.113.10");
        var responses = new List<HttpResponseMessage>();

        for (var i = 0; i < 25; i++)
        {
            var response = await client.PostAsJsonAsync("/api/faq/ask", new { query = $"test query number {i}" });
            responses.Add(response);
        }

        Assert.Contains(responses, r => r.StatusCode == (HttpStatusCode)429);

        var throttled = responses.First(r => r.StatusCode == (HttpStatusCode)429);
        Assert.True(throttled.Headers.RetryAfter is not null, "429 response must include Retry-After header.");
    }

    [Fact]
    public async Task QuotationEndpoint_BurstAboveLimit_Returns429WithRetryAfter()
    {
        var client = await WithDevTokenAsync(CreateClientWithIp("203.0.113.20"));
        var responses = new List<HttpResponseMessage>();

        for (var i = 0; i < 25; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/quotation/quote", new { accountId = "ACC-LOAD-TEST" });
            responses.Add(response);
        }

        Assert.Contains(responses, r => r.StatusCode == (HttpStatusCode)429);

        var throttled = responses.First(r => r.StatusCode == (HttpStatusCode)429);
        Assert.True(throttled.Headers.RetryAfter is not null, "429 response must include Retry-After header.");
    }

    [Fact]
    public async Task TwoDifferentIPs_EachGetOwnLimit_NotGloballyShared()
    {
        var clientA = CreateClientWithIp("203.0.113.30");
        var clientB = CreateClientWithIp("203.0.113.31");

        for (var i = 0; i < 20; i++)
            await clientA.PostAsJsonAsync("/api/faq/ask", new { query = $"client a message {i}" });

        var aResponse = await clientA.PostAsJsonAsync("/api/faq/ask", new { query = "client a final message" });
        var bResponse = await clientB.PostAsJsonAsync("/api/faq/ask", new { query = "client b first message" });

        Assert.Equal((HttpStatusCode)429, aResponse.StatusCode);
        Assert.NotEqual((HttpStatusCode)429, bResponse.StatusCode);
    }

    [Fact]
    public async Task FaqAndQuotation_HaveIndependentLimits_NotASharedBucket()
    {
        var client = await WithDevTokenAsync(CreateClientWithIp("203.0.113.40"));

        for (var i = 0; i < 20; i++)
            await client.PostAsJsonAsync("/api/quotation/quote", new { accountId = $"ACC-{i}" });

        var quotationResponse = await client.PostAsJsonAsync(
            "/api/quotation/quote", new { accountId = "ACC-OVER" });
        var faqResponse = await client.PostAsJsonAsync(
            "/api/faq/ask", new { query = "should still work fine" });

        Assert.Equal((HttpStatusCode)429, quotationResponse.StatusCode);
        Assert.NotEqual((HttpStatusCode)429, faqResponse.StatusCode);
    }

    
}