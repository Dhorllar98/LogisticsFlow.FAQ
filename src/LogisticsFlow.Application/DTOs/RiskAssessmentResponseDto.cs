namespace LogisticsFlow.Application.DTOs;

public class RiskAssessmentResponseDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public double ElapsedDays { get; set; }
    public double? LaneAverageDays { get; set; }
    public int SampleSize { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
}