namespace LogisticsFlow.Domain.Entities;

/// <summary>
/// A negotiated rate agreement tied to a specific client. Per the
/// "no address reuse expected" decision (Phase 2 kickoff), origin and
/// destination addresses are stored flat on the agreement rather than
/// normalized into a separate address entity. Revisit only if a real
/// "saved default address" requirement surfaces in a later phase.
///
/// Tier 2 fields: OriginAddress, DestinationAddress,
/// SpecialHandlingInstructions, NegotiatedRate — see
/// docs/data-classification.md and CLAUDE.md Phase 2 section.
/// </summary>
public class RateAgreement
{
    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }

    public string OriginAddress { get; private set; } = string.Empty;
    public string DestinationAddress { get; private set; } = string.Empty;
    public string? SpecialHandlingInstructions { get; private set; }
    public decimal NegotiatedRate { get; private set; }

    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    private RateAgreement() { } // EF Core

    public RateAgreement(
        Guid id,
        Guid clientId,
        string originAddress,
        string destinationAddress,
        decimal negotiatedRate,
        DateTime effectiveFrom,
        string? specialHandlingInstructions = null,
        DateTime? effectiveTo = null)
    {
        if (string.IsNullOrWhiteSpace(originAddress))
            throw new ArgumentException("OriginAddress is required.", nameof(originAddress));
        if (string.IsNullOrWhiteSpace(destinationAddress))
            throw new ArgumentException("DestinationAddress is required.", nameof(destinationAddress));
        if (negotiatedRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(negotiatedRate), "Rate must be positive.");
        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
            throw new ArgumentException("EffectiveTo cannot precede EffectiveFrom.", nameof(effectiveTo));

        Id = id;
        ClientId = clientId;
        OriginAddress = originAddress;
        DestinationAddress = destinationAddress;
        NegotiatedRate = negotiatedRate;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        SpecialHandlingInstructions = specialHandlingInstructions;
    }

    public bool IsCurrentlyEffective(DateTime asOfUtc) =>
        asOfUtc >= EffectiveFrom && (!EffectiveTo.HasValue || asOfUtc <= EffectiveTo.Value);
}
