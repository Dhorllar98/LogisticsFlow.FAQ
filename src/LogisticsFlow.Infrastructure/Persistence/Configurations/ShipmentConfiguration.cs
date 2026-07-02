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
            .HasMaxLength(50);

        builder.Property(s => s.OriginAddress)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(s => s.DestinationAddress)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(s => s.ConsigneeName)
            .HasMaxLength(256);

        builder.Property(s => s.ConsigneeAddress)
            .HasMaxLength(512);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        // Real FK now, matching RateAgreement -> Client exactly.
        // Restrict, not Cascade — deleting a Client must never silently
        // wipe shipment history, same reasoning your RateAgreement
        // config already applied.
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