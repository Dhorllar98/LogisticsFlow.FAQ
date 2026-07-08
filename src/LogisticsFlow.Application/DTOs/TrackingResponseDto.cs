namespace LogisticsFlow.Application.DTOs;

public class TrackingResponseDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string StatusSummary { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; }
}