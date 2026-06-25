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
    }
}
