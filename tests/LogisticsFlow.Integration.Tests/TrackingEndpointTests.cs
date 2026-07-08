using System.Net;
using System.Net.Http.Json;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

public class TrackingEndpointTests : IClassFixture<TestApiFactory>
{
    private sealed class FakeTrackingLlmClient : ILlmClient
    {
        public Task<string> SendMessageAsync(
            string systemPrompt, IReadOnlyList<ChatMessage> conversationHistory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Your shipment departed origin and is currently in transit.");
        }
    }

    private readonly WebApplicationFactory<Program> _factory;

    public TrackingEndpointTests(TestApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILlmClient>();
                services.AddSingleton<ILlmClient, FakeTrackingLlmClient>();
            });
        });
    }

    private sealed record TokenResponse(string Token);
    private sealed record TrackingResponsePayload(string TrackingNumber, string Carrier, string Mode, string StatusSummary, DateTime LastUpdatedUtc);

    /// <summary>
    /// Seeds its own Client + Shipment via AppDbContext rather than
    /// depending on manually-inserted demo data from a prior psql
    /// session - that data only exists on one developer's machine and
    /// is not reproducible on a fresh clone or in CI. Unique
    /// AccountId/TrackingNumber per call avoids collisions across runs
    /// against the same real Postgres instance.
    /// </summary>
    private async Task<(string accountId, string secret, string trackingNumber)> SeedShipmentAsync()
    {
        var accountId = $"ACC-TEST-{Guid.NewGuid():N}"[..20];
        var trackingNumber = $"TRK-TEST-{Guid.NewGuid():N}"[..15];
        const string secret = "test-secret-001";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = new Client(Guid.NewGuid(), accountId, "Test Freight Co",
            BCrypt.Net.BCrypt.HashPassword(secret));
        db.Clients.Add(client);

        db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            ClientId = client.Id,
            Carrier = "Maersk Line",
            Mode = ShipmentMode.Sea,
            OriginAddress = "123 Dock Rd, Lagos",
            DestinationAddress = "45 Port Ave, Apapa",
            ConsigneeName = "John Doe",
            ConsigneeAddress = "45 Port Ave, Apapa, Lagos",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            Events = new List<TrackingEvent>
            {
                new() { Id = Guid.NewGuid(), MilestoneType = "DepartedOrigin", Location = "Lagos Port", TimestampUtc = DateTime.UtcNow }
            }
        });

        await db.SaveChangesAsync();
        return (accountId, secret, trackingNumber);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string accountId, string secret)
    {
        var client = _factory.CreateClient();
        var tokenResponse = await client.PostAsJsonAsync("/api/quotation/token", new { accountId, secret });
        var token = (await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    [Fact]
    public async Task GetStatus_NoToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tracking/status", new { trackingNumber = "TRK-ANY" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_ValidTokenKnownTrackingNumber_Returns200WithComposedSummary()
    {
        var (accountId, secret, trackingNumber) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(accountId, secret);

        var response = await client.PostAsJsonAsync("/api/tracking/status", new { trackingNumber });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TrackingResponsePayload>();
        Assert.False(string.IsNullOrWhiteSpace(body!.StatusSummary));
    }

    [Fact]
    public async Task GetStatus_TrackingNumberBelongsToDifferentAccount_Returns404()
    {
        var (_, _, trackingNumber) = await SeedShipmentAsync();
        var (otherAccountId, otherSecret, _) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(otherAccountId, otherSecret);

        var response = await client.PostAsJsonAsync("/api/tracking/status", new { trackingNumber });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_UnknownTrackingNumber_Returns404()
    {
        var (accountId, secret, _) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(accountId, secret);

        var response = await client.PostAsJsonAsync("/api/tracking/status", new { trackingNumber = "TRK-DOES-NOT-EXIST" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_BurstAboveLimit_Returns429WithRetryAfter()
    {
        var (accountId, secret, trackingNumber) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(accountId, secret);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.50");

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 25; i++)
            responses.Add(await client.PostAsJsonAsync("/api/tracking/status", new { trackingNumber }));

        Assert.Contains(responses, r => r.StatusCode == (HttpStatusCode)429);
        var throttled = responses.First(r => r.StatusCode == (HttpStatusCode)429);
        Assert.True(throttled.Headers.RetryAfter is not null, "429 response must include Retry-After header.");
    }
}