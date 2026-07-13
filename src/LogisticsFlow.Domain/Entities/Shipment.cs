using LogisticsFlow.Domain.Enums;

namespace LogisticsFlow.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public ShipmentMode Mode { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string OriginRegion { get; set; } = string.Empty;
    public string DestinationRegion { get; set; } = string.Empty;
    public string ConsigneeName { get; set; } = string.Empty;
    public string ConsigneeAddress { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
}