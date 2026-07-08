using LogisticsFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsFlow.Infrastructure.Persistence.Configurations;

public class TrackingEventConfiguration : IEntityTypeConfiguration<TrackingEvent>
{
    public void Configure(EntityTypeBuilder<TrackingEvent> builder)
    {
        builder.ToTable("TrackingEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MilestoneType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TimestampUtc)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(1024);

        builder.HasIndex(e => new { e.ShipmentId, e.TimestampUtc });
    }
}