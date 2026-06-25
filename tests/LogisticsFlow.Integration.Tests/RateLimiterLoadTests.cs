using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

/// <summary>
/// Closes the Phase 1 security-hardening-checklist.md Section 7 gap
/// ("Rate limiter load-tested, not just unit-tested"). Covers both
/// /api/faq/ask and /api/quotation/quote, which now have separate named
/// policies (faq-limit / quotation-limit) per the resolved "shared
/// policy" flag.
///
/// RESOLVED (was flagged): per-IP partitioning now reads X-Forwarded-For
/// first (see RateLimitingExtensions.ResolveClientIp), which is also the
/// real production fix needed once deployed behind Railway/Render's
/// reverse proxy. These tests set that header explicitly per simulated
/// client, so the per-IP isolation test is now a genuine assertion, not
/// one that happened to pass because TestServer gives every client the
/// same loopback address.
/// </summary>
public class RateLimiterLoadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimiterLoadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
        // Per CLAUDE.md: 20 requests/IP/minute on /api/faq/ask.
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
        // Regression guard for the Phase 1 "per-IP, not global" fix, now
        // verified through the same X-Forwarded-For path production
        // traffic will actually use behind a reverse proxy.
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
        // Confirms the two endpoints no longer share "faq-limit" as a
        // side effect - exhausting Quotation's policy must not affect
        // the FAQ endpoint for the same simulated IP.
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
