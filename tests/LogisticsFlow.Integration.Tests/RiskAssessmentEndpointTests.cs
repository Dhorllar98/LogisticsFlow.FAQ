using System.Net;
using System.Net.Http.Json;
using LogisticsFlow.Domain.Constants;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

public class RiskAssessmentEndpointTests : IClassFixture<TestApiFactory>
{
    private sealed class FakeRiskAssessmentLlmClient : ILlmClient
    {
        public Task<string> SendMessageAsync(
            string systemPrompt, IReadOnlyList<ChatMessage> conversationHistory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("Your shipment is progressing as expected for this route.");
        }
    }

    private readonly WebApplicationFactory<Program> _factory;

    public RiskAssessmentEndpointTests(TestApiFactory factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILlmClient>();
                services.AddSingleton<ILlmClient, FakeRiskAssessmentLlmClient>();
            });
        });
    }

    private sealed record TokenResponse(string Token);
    private sealed record RiskAssessmentResponsePayload(
        string TrackingNumber, string Carrier, string Mode, double ElapsedDays,
        double? LaneAverageDays, int SampleSize, string RiskLevel, string SuggestedAction);

    private async Task<(string accountId, string secret, string trackingNumber)> SeedShipmentAsync(
        bool isDelivered = false, bool useUniqueLane = false)
    {
        var accountId = $"ACC-TEST-{Guid.NewGuid():N}"[..20];
        var trackingNumber = $"TRK-TEST-{Guid.NewGuid():N}"[..15];
        const string secret = "test-secret-001";

        // Tests asserting "no lane history" must use a lane no other
        // seeded data (demo or otherwise) could ever collide with -
        // a shared fixed lane like "Lagos"/"Apapa" is not safe here since
        // integration tests run against a real, persistent dev database.
        var carrier = useUniqueLane ? $"TestCarrier-{Guid.NewGuid():N}"[..12] : "Maersk Line";
        var originRegion = useUniqueLane ? $"TestOrigin-{Guid.NewGuid():N}"[..12] : "Lagos";
        var destinationRegion = useUniqueLane ? $"TestDest-{Guid.NewGuid():N}"[..12] : "Apapa";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = new Client(Guid.NewGuid(), accountId, "Test Freight Co",
            BCrypt.Net.BCrypt.HashPassword(secret));
        db.Clients.Add(client);

        var events = new List<TrackingEvent>
        {
            new() { Id = Guid.NewGuid(), MilestoneType = "DepartedOrigin", Location = originRegion, TimestampUtc = DateTime.UtcNow.AddDays(-3) }
        };

        if (isDelivered)
        {
            events.Add(new TrackingEvent
            {
                Id = Guid.NewGuid(),
                MilestoneType = MilestoneTypes.Delivered,
                Location = destinationRegion,
                TimestampUtc = DateTime.UtcNow
            });
     }

        db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            ClientId = client.Id,
            Carrier = carrier,
            Mode = ShipmentMode.Sea,
            OriginAddress = $"Address in {originRegion}",
            DestinationAddress = $"Address in {destinationRegion}",
            OriginRegion = originRegion,
            DestinationRegion = destinationRegion,
            ConsigneeName = "John Doe",
            ConsigneeAddress = $"Address in {destinationRegion}",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
            Events = events
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
    public async Task GetRiskAssessment_NoToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tracking/risk-assessment", new { trackingNumber = "TRK-ANY" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRiskAssessment_UnknownTrackingNumber_Returns404()
    {
        var (accountId, secret, _) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(accountId, secret);

        var response = await client.PostAsJsonAsync("/api/tracking/risk-assessment", new { trackingNumber = "TRK-DOES-NOT-EXIST" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRiskAssessment_TrackingNumberBelongsToDifferentAccount_Returns404()
    {
        var (_, _, trackingNumber) = await SeedShipmentAsync();
        var (otherAccountId, otherSecret, _) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(otherAccountId, otherSecret);

        var response = await client.PostAsJsonAsync("/api/tracking/risk-assessment", new { trackingNumber });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRiskAssessment_ValidTokenInTransitShipment_Returns200WithUnknownRiskWhenNoLaneHistory()
    {
        var (accountId, secret, trackingNumber) = await SeedShipmentAsync(isDelivered: false, useUniqueLane: true);
        var client = await AuthenticatedClientAsync(accountId, secret);

        var response = await client.PostAsJsonAsync("/api/tracking/risk-assessment", new { trackingNumber });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RiskAssessmentResponsePayload>();
        Assert.False(string.IsNullOrWhiteSpace(body!.SuggestedAction));
        Assert.Equal("Unknown", body.RiskLevel);
    }

    [Fact]
    public async Task GetRiskAssessment_BurstAboveLimit_Returns429WithRetryAfter()
    {
        var (accountId, secret, trackingNumber) = await SeedShipmentAsync();
        var client = await AuthenticatedClientAsync(accountId, secret);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.51");

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 25; i++)
            responses.Add(await client.PostAsJsonAsync("/api/tracking/risk-assessment", new { trackingNumber }));

        Assert.Contains(responses, r => r.StatusCode == (HttpStatusCode)429);
        var throttled = responses.First(r => r.StatusCode == (HttpStatusCode)429);
        Assert.True(throttled.Headers.RetryAfter is not null, "429 response must include Retry-After header.");
    }

    [Fact]
    public async Task GetRiskAssessment_DeliveredShipment_Returns200WithNormalRisk()
    {
        var (accountId, secret, trackingNumber) = await SeedShipmentAsync(isDelivered: true);
        var client = await AuthenticatedClientAsync(accountId, secret);

        var response = await client.PostAsJsonAsync("/api/tracking/risk-assessment", new { trackingNumber });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RiskAssessmentResponsePayload>();
        Assert.Equal("Normal", body!.RiskLevel);
    }
}