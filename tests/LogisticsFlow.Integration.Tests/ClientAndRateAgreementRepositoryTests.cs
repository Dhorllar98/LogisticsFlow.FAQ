using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Infrastructure.Persistence;
using LogisticsFlow.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

public class ClientAndRateAgreementRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ClientAndRateAgreementRepositoryTests()
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

    [Fact]
    public async Task ClientRepository_GetByAccountId_ReturnsSeededClient()
    {
        var client = new Client(Guid.NewGuid(), "ACC-INT-1", "Integration Test Freight Co", "test-secret-hash-1");
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var repo = new ClientRepository(_db);
        var found = await repo.GetByAccountIdAsync("ACC-INT-1");

        Assert.NotNull(found);
        Assert.Equal(client.CompanyName, found!.CompanyName);
    }

    [Fact]
    public async Task ClientRepository_GetByAccountId_UnknownAccount_ReturnsNull()
    {
        var repo = new ClientRepository(_db);
        var found = await repo.GetByAccountIdAsync("does-not-exist");

        Assert.Null(found);
    }

    [Fact]
    public async Task RateAgreementRepository_GetCurrentForClient_ReturnsOnlyEffectiveAgreement()
    {
        var client = new Client(Guid.NewGuid(), "ACC-INT-2", "Another Freight Co", "test-secret-hash-2");
        _db.Clients.Add(client);

        var expired = new RateAgreement(
            Guid.NewGuid(), client.Id, "Old Origin", "Old Dest", 100m,
            effectiveFrom: DateTime.UtcNow.AddDays(-30), effectiveTo: DateTime.UtcNow.AddDays(-1));

        var current = new RateAgreement(
            Guid.NewGuid(), client.Id, "Current Origin", "Current Dest", 250m,
            effectiveFrom: DateTime.UtcNow.AddDays(-1));

        _db.RateAgreements.AddRange(expired, current);
        await _db.SaveChangesAsync();

        var repo = new RateAgreementRepository(_db);
        var result = await repo.GetCurrentForClientAsync(client.Id);

        Assert.NotNull(result);
        Assert.Equal(current.Id, result!.Id);
    }

    [Fact]
    public async Task RateAgreementRepository_NoAgreementForClient_ReturnsNull()
    {
        var repo = new RateAgreementRepository(_db);
        var result = await repo.GetCurrentForClientAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
