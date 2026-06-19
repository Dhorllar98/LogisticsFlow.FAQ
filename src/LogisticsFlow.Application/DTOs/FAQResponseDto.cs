using LogisticsFlow.Domain.Enums;

namespace LogisticsFlow.Application.DTOs;

/// <summary>
/// The shaped, validated response returned to the client. EscalationBoolean
/// is always computed server-side from ConfidenceScore and GroundingSources
/// — it is never trusted directly from the AI's raw output. See
/// docs/architecture.md, "Confidence and Escalation Logic".
/// </summary>
public class FAQResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public LogisticCategory Category { get; set; } = LogisticCategory.General;
    public double ConfidenceScore { get; set; }
    public bool EscalationBoolean { get; set; }
    public List<string> GroundingSources { get; set; } = new();
    public Guid SessionId { get; set; }
}