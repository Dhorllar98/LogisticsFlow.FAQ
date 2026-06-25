namespace LogisticsFlow.Application.DTOs;

/// <summary>
/// Outgoing quotation response. Fields below are real (unredacted) Tier 2
/// values — redaction only applies to the data sent to the cloud LLM, not
/// to the response returned to the client who owns that data.
/// </summary>
public class QuotationResponseDto
{
    public Guid ClientId { get; set; }
    public decimal NegotiatedRate { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string? SpecialHandlingInstructions { get; set; }

    /// <summary>
    /// Claude-composed customer-facing message (e.g. a friendly summary
    /// of the quote). Restored from redacted tokens before being set here.
    /// </summary>
    public string ComposedMessage { get; set; } = string.Empty;
}
