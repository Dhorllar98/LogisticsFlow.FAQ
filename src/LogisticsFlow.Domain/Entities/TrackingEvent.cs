namespace LogisticsFlow.Domain.Entities;

public class TrackingEvent
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string MilestoneType { get; set; } = string.Empty; // e.g. "DepartedOrigin"
    public string Location { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? Notes { get; set; }
}