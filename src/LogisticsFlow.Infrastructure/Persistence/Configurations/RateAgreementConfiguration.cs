using LogisticsFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogisticsFlow.Infrastructure.Persistence.Configurations;

public class RateAgreementConfiguration : IEntityTypeConfiguration<RateAgreement>
{
    public void Configure(EntityTypeBuilder<RateAgreement> builder)
    {
        builder.ToTable("RateAgreements");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.OriginAddress)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(r => r.DestinationAddress)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(r => r.SpecialHandlingInstructions)
            .HasMaxLength(1024);

        builder.Property(r => r.NegotiatedRate)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(r => r.EffectiveFrom).IsRequired();
        builder.Property(r => r.EffectiveTo);

        builder.HasIndex(r => new { r.ClientId, r.EffectiveFrom });

        // No navigation property exposed on Client by design — Domain
        // entities here are intentionally not bidirectionally linked;
        // RateAgreementRepository queries by ClientId (FK) directly.
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(new RateAgreement(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            clientId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            originAddress: "123 Dock Rd, Lagos",
            destinationAddress: "45 Port Ave, Apapa",
            negotiatedRate: 1500.00m,
            effectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            specialHandlingInstructions: "Fragile - keep upright"));
    }
}
