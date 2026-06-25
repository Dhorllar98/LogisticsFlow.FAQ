using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LogisticsFlow.Infrastructure.Persistence;

/// <summary>
/// First EF Core DbContext in the suite — Phase 1 (FAQ) was JSON-backed
/// and had no database. Fluent API only, per s-tier-backend standards;
/// no DataAnnotations on entities. Provider is SQL Server (configured in
/// DependencyInjection.cs via UseSqlServer).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<RateAgreement> RateAgreements => Set<RateAgreement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new RateAgreementConfiguration());
    }
}
