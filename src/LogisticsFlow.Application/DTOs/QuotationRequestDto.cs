namespace LogisticsFlow.Application.DTOs;

/// <summary>
/// Incoming request for a quotation. AccountId is deliberately NOT here —
/// account identity comes from the authenticated JWT's claims, extracted
/// in QuotationController, never from client-supplied input. A prior
/// version of this DTO carried AccountId as a request field, which meant
/// [Authorize] only proved the caller held *some* valid token, never that
/// they were requesting their own account's data — any authenticated
/// client could read any other client's negotiated rate and addresses by
/// simply changing this field. Fixed by removing it entirely; see
/// TrackingRequestDto for the same pattern already in place there.
/// </summary>
public class QuotationRequestDto
{
    /// <summary>
    /// Optional free-text context from the customer (e.g. "can you also
    /// confirm the handling instructions?"). Treated as untrusted input
    /// per the standing security instruction in CLAUDE.md — never
    /// templated into the system prompt.
    /// </summary>
    public string? CustomerQuery { get; set; }
}