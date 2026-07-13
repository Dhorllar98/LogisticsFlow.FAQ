using LogisticsFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsFlow.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TrackingNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.TrackingNumber)
            .IsUnique();

        builder.Property(s => s.Carrier)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.Mode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.OriginAddress)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(s => s.DestinationAddress)
            .IsRequired()
            .HasMaxLength(512);

        // Coarse, non-account-identifying grouping keys for lane-history
        // aggregation (Phase 3.5). Populated at creation time going
        // forward; existing rows backfilled once via migration SQL, not
        // parsed at query time - see CLAUDE.md Phase 3.5 section.
        builder.Property(s => s.OriginRegion)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.DestinationRegion)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(s => new { s.Carrier, s.Mode, s.OriginRegion, s.DestinationRegion });

        builder.Property(s => s.ConsigneeName)
            .HasMaxLength(256);

        builder.Property(s => s.ConsigneeAddress)
            .HasMaxLength(512);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ClientId);

        builder.HasMany(s => s.Events)
            .WithOne()
            .HasForeignKey(e => e.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}