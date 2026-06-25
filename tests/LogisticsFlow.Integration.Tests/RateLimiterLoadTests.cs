using System.Net;
using System.Net.Http.Json;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

/// <summary>
/// RESOLVED (Finding C): the original version of this test hit the real
/// Claude API on every request. With a 1-minute fixed window and ~4
/// seconds per real Claude call, 25 sequential requests took nearly 3
/// minutes - long enough for the window to reset mid-burst, meaning the
/// limiter never actually saturated and the 429 assertions failed for
/// reasons unrelated to the limiter itself. FakeClaudeApiClient below
/// replaces the real one for these tests only, so requests complete in
/// milliseconds and the limiter is what's actually being tested.
/// </summary>
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
        // Note: this endpoint will hit a real (likely failing) DB
        // connection since no connection string/migration exists yet at
        // this stage of Phase 2 - that's fine for THIS test, since the
        // rate limiter runs before the controller's business logic and
        // will still correctly reject the 21st+ request regardless of
        // what the controller does afterward.
        var client = CreateClientWithIp("203.0.113.20");
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
        var client = CreateClientWithIp("203.0.113.40");

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