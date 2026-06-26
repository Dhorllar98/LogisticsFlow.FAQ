using LogisticsFlow.Domain.ValueObjects;

namespace LogisticsFlow.Domain.Interfaces;

/// <summary>
/// Contract for Tier 2 redact/restore. Implementation (Presidio or a
/// stand-in) lives in Infrastructure. Per the locked Phase 2 decision,
/// Application — not Infrastructure — holds the returned RedactionMap
/// for the request lifetime; this service is a stateless mapper.
/// </summary>
public interface IRedactionService
{
    Task<(string RedactedText, RedactionMap Map)> RedactAsync(
        string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores original values into text using the supplied map. Throws
    /// RedactionFailureException if a token in the text has no entry in
    /// the map — this must surface as a hard 422, never a partial/leaked
    /// response, per CLAUDE.md.
    /// </summary>
    Task<string> RestoreAsync(
        string redactedText, RedactionMap map, CancellationToken cancellationToken = default);
}
