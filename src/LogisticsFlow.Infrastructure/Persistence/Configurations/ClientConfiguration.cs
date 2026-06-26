using LogisticsFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsFlow.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccountId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(c => c.AccountId)
            .IsUnique();

        builder.Property(c => c.CompanyName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.HasData(new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AccountId = "ACC-DEMO-001",
                CompanyName = "Acme Freight Ltd",
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
    }
}
