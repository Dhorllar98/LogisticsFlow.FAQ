namespace LogisticsFlow.Application.DTOs;

/// <summary>
/// Lightweight summary of one rate agreement, used to let a multi-
/// agreement account choose which one to quote against before calling
/// POST /api/quotation/quote. Deliberately excludes SpecialHandling
/// Instructions and ComposedMessage - this is a selection list, not a
/// full quote; the full Tier 2 detail only returns from the quote
/// endpoint itself, scoped to the one agreement actually chosen.
/// </summary>
public class RateAgreementSummaryDto
{
    public Guid AgreementId { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public decimal NegotiatedRate { get; set; }
}