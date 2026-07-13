using LogisticsFlow.Domain.Constants;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Infrastructure.Persistence;
using LogisticsFlow.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

public class LaneHistoryRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public LaneHistoryRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Client> SeedClientAsync(string accountId)
    {
        var client = new Client(Guid.NewGuid(), accountId, "Test Freight Co", "test-secret-hash");
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return client;
    }

    private async Task SeedShipmentAsync(
        Guid clientId, string trackingNumber, string carrier, ShipmentMode mode,
        string originRegion, string destinationRegion,
        DateTime createdAtUtc, DateTime? deliveredAtUtc)
    {
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingNumber = trackingNumber,
            ClientId = clientId,
            Carrier = carrier,
            Mode = mode,
            OriginAddress = $"Address for {originRegion}",
            DestinationAddress = $"Address for {destinationRegion}",
            OriginRegion = originRegion,
            DestinationRegion = destinationRegion,
            ConsigneeName = "Test Consignee",
            ConsigneeAddress = "Test Consignee Address",
            CreatedAtUtc = createdAtUtc,
            Events = new List<TrackingEvent>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    MilestoneType = "DepartedOrigin",
                    Location = originRegion,
                    TimestampUtc = createdAtUtc.AddHours(1)
                }
            }
        };

        if (deliveredAtUtc.HasValue)
        {
            shipment.Events.Add(new TrackingEvent
            {
                Id = Guid.NewGuid(),
                MilestoneType = MilestoneTypes.Delivered,
                Location = destinationRegion,
                TimestampUtc = deliveredAtUtc.Value
            });
        }

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLaneStatsAsync_FewerThanFiveDeliveredShipments_ReturnsNull()
    {
        var client = await SeedClientAsync("ACC-LANE-1");
        var baseTime = DateTime.UtcNow.AddDays(-30);

        for (var i = 0; i < 4; i++)
        {
            await SeedShipmentAsync(
                client.Id, $"TRK-LANE1-{i}", "Maersk Line", ShipmentMode.Sea,
                "Lagos", "Apapa",
                baseTime.AddDays(i), baseTime.AddDays(i).AddDays(4));
        }

        var repo = new LaneHistoryRepository(_db);
        var result = await repo.GetLaneStatsAsync("Maersk Line", ShipmentMode.Sea, "Lagos", "Apapa", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLaneStatsAsync_ExactlyFiveDeliveredShipments_ReturnsAverage()
    {
        var client = await SeedClientAsync("ACC-LANE-2");
        var baseTime = DateTime.UtcNow.AddDays(-30);
        var transitDays = new[] { 3, 4, 4, 5, 4 };

        for (var i = 0; i < 5; i++)
        {
            await SeedShipmentAsync(
                client.Id, $"TRK-LANE2-{i}", "Maersk Line", ShipmentMode.Sea,
                "Lagos", "Apapa",
                baseTime.AddDays(i * 5), baseTime.AddDays(i * 5).AddDays(transitDays[i]));
        }

        var repo = new LaneHistoryRepository(_db);
        var result = await repo.GetLaneStatsAsync("Maersk Line", ShipmentMode.Sea, "Lagos", "Apapa", default);

        Assert.NotNull(result);
        Assert.Equal(5, result!.SampleSize);
        Assert.Equal(4.0, result.AverageTransitDays, precision: 1);
    }

    [Fact]
    public async Task GetLaneStatsAsync_InTransitShipmentsExcludedFromAverage()
    {
        var client = await SeedClientAsync("ACC-LANE-3");
        var baseTime = DateTime.UtcNow.AddDays(-30);

        // 5 delivered, all exactly 4 days
        for (var i = 0; i < 5; i++)
        {
            await SeedShipmentAsync(
                client.Id, $"TRK-LANE3-DELIVERED-{i}", "Maersk Line", ShipmentMode.Sea,
                "Lagos", "Apapa",
                baseTime.AddDays(i * 5), baseTime.AddDays(i * 5).AddDays(4));
        }

        // 3 more, still in transit - should not affect the average at all
        for (var i = 0; i < 3; i++)
        {
            await SeedShipmentAsync(
                client.Id, $"TRK-LANE3-INTRANSIT-{i}", "Maersk Line", ShipmentMode.Sea,
                "Lagos", "Apapa",
                DateTime.UtcNow.AddDays(-1), deliveredAtUtc: null);
        }

        var repo = new LaneHistoryRepository(_db);
        var result = await repo.GetLaneStatsAsync("Maersk Line", ShipmentMode.Sea, "Lagos", "Apapa", default);

        Assert.NotNull(result);
        Assert.Equal(5, result!.SampleSize);
        Assert.Equal(4.0, result.AverageTransitDays, precision: 1);
    }

    [Fact]
    public async Task GetLaneStatsAsync_DifferentLaneShipmentsDoNotContaminateAverage()
    {
        var client = await SeedClientAsync("ACC-LANE-4");
        var baseTime = DateTime.UtcNow.AddDays(-30);

        // 5 delivered on Lagos -> Apapa, all 4 days
        for (var i = 0; i < 5; i++)
        {
            await SeedShipmentAsync(
                client.Id, $"TRK-LANE4-A-{i}", "Maersk Line", ShipmentMode.Sea,
                "Lagos", "Apapa",
                baseTime.AddDays(i * 5), baseTime.AddDays(i * 5).AddDays(4));
        }

        // 5 delivered on a different lane (Kano -> Lagos), all 10 days -
        // must not blend into the Lagos->Apapa average
        for (var i = 0; i < 5; i++)
        {
            await SeedShipmentAsync(
                client.Id, $"TRK-LANE4-B-{i}", "Maersk Line", ShipmentMode.Sea,
                "Kano", "Lagos",
                baseTime.AddDays(i * 5), baseTime.AddDays(i * 5).AddDays(10));
        }

        var repo = new LaneHistoryRepository(_db);
        var result = await repo.GetLaneStatsAsync("Maersk Line", ShipmentMode.Sea, "Lagos", "Apapa", default);

        Assert.NotNull(result);
        Assert.Equal(5, result!.SampleSize);
        Assert.Equal(4.0, result.AverageTransitDays, precision: 1);
    }

    [Fact]
    public async Task GetLaneStatsAsync_PoolsAcrossMultipleClientsOnSameLane()
    {
        // Proves cross-account pooling works by design (Option C) - not
        // just cross-account exclusion like Tracking's own lookup.
        var clientA = await SeedClientAsync("ACC-LANE-5A");
        var clientB = await SeedClientAsync("ACC-LANE-5B");
        var baseTime = DateTime.UtcNow.AddDays(-30);

        await SeedShipmentAsync(clientA.Id, "TRK-LANE5-A1", "Maersk Line", ShipmentMode.Sea,
            "Lagos", "Apapa", baseTime, baseTime.AddDays(4));
        await SeedShipmentAsync(clientA.Id, "TRK-LANE5-A2", "Maersk Line", ShipmentMode.Sea,
            "Lagos", "Apapa", baseTime.AddDays(5), baseTime.AddDays(5).AddDays(4));
        await SeedShipmentAsync(clientA.Id, "TRK-LANE5-A3", "Maersk Line", ShipmentMode.Sea,
            "Lagos", "Apapa", baseTime.AddDays(10), baseTime.AddDays(10).AddDays(4));

        await SeedShipmentAsync(clientB.Id, "TRK-LANE5-B1", "Maersk Line", ShipmentMode.Sea,
            "Lagos", "Apapa", baseTime.AddDays(15), baseTime.AddDays(15).AddDays(4));
        await SeedShipmentAsync(clientB.Id, "TRK-LANE5-B2", "Maersk Line", ShipmentMode.Sea,
            "Lagos", "Apapa", baseTime.AddDays(20), baseTime.AddDays(20).AddDays(4));

        var repo = new LaneHistoryRepository(_db);
        var result = await repo.GetLaneStatsAsync("Maersk Line", ShipmentMode.Sea, "Lagos", "Apapa", default);

        Assert.NotNull(result);
        Assert.Equal(5, result!.SampleSize);
        Assert.Equal(4.0, result.AverageTransitDays, precision: 1);
    }
}