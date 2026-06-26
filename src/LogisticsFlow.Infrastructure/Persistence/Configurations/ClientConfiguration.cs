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

        builder.Property(c => c.SecretHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        // Demo client's raw secret is "demo-secret-001" - this is the
        // BCrypt hash of that string, generated once and pinned here so
        // the migration is deterministic. Real clients get a fresh
        // randomly-generated secret at onboarding time (not built yet -
        // out of scope for this phase, same as the rest of client
        // onboarding).
        builder.HasData(new
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            AccountId = "ACC-DEMO-001",
            CompanyName = "Acme Freight Ltd",
            SecretHash = "$2a$11$qHMa4rcIKtMtFvqhKX3ryuXsrrNF3d6hNcaJVB6tHPnnZHGxCSdVG",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}