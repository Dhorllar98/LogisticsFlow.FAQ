namespace LogisticsFlow.Domain.Constants;

/// <summary>
/// Well-known TrackingEvent.MilestoneType values with cross-layer
/// meaning. MilestoneType itself remains free-text (not an enum) since
/// carriers report many milestone types and the set is open-ended - a
/// full enum conversion is a reasonable future candidate (mirroring the
/// ShipmentMode fix) if this grows, but is out of scope for Phase 3.5.
/// Lives in Domain, not Infrastructure, so both Application and
/// Infrastructure can reference it without violating the dependency
/// chain (Application must never depend on Infrastructure).
/// </summary>
public static class MilestoneTypes
{
    public const string Delivered = "Delivered";
}