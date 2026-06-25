namespace LogisticsFlow.Application.DTOs;

/// <summary>
/// Incoming request for a quotation. AccountId identifies which client's
/// rate agreement to look up — this is how a Quotation query becomes
/// Tier 2 (it refers to identifiable business data), unlike Phase 1's
/// anonymous FAQ queries.
/// </summary>
public class QuotationRequestDto
{
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Optional free-text context from the customer (e.g. "can you also
    /// confirm the handling instructions?"). Treated as untrusted input
    /// per the standing security instruction in CLAUDE.md — never
    /// templated into the system prompt.
    /// </summary>
    public string? CustomerQuery { get; set; }
}
