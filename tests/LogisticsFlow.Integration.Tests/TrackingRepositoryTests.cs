using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Infrastructure.Persistence;
using LogisticsFlow.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

public class TrackingRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public TrackingRepositoryTests()
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

    private async Task<(Client client, Shipment shipment)> SeedShipmentAsync(string accountId, string trackingNumber)
    {
        var client = new Client(Guid.NewGuid(), accountId, "Test Freight Co", "test-secret-hash");
        _db.Clients.Add(client);

        var shipment = new Shipment
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
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
            Events = new List<TrackingEvent>
            {
                new() { Id = Guid.NewGuid(), MilestoneType = "DepartedOrigin", Location = "Lagos Port", TimestampUtc = DateTime.UtcNow.AddDays(-2) }
            }
        };
        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        return (client, shipment);
    }

    [Fact]
    public async Task GetByTrackingNumberForAccountAsync_MatchingAccount_ReturnsShipmentWithEvents()
    {
        var (client, shipment) = await SeedShipmentAsync("ACC-INT-TRK-1", "TRK-INT-001");

        var repo = new TrackingRepository(_db);
        var result = await repo.GetByTrackingNumberForAccountAsync("TRK-INT-001", client.AccountId, default);

        Assert.NotNull(result);
        Assert.Equal(shipment.Id, result!.Id);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task GetByTrackingNumberForAccountAsync_WrongAccount_ReturnsNull()
    {
        await SeedShipmentAsync("ACC-INT-TRK-2", "TRK-INT-002");

        var repo = new TrackingRepository(_db);
        var result = await repo.GetByTrackingNumberForAccountAsync("TRK-INT-002", "ACC-WRONG-ACCOUNT", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTrackingNumberForAccountAsync_UnknownTrackingNumber_ReturnsNull()
    {
        var repo = new TrackingRepository(_db);
        var result = await repo.GetByTrackingNumberForAccountAsync("TRK-DOES-NOT-EXIST", "ACC-ANY", default);

        Assert.Null(result);
    }
}