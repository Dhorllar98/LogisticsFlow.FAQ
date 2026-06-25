using System.Text.RegularExpressions;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Domain.ValueObjects;

namespace LogisticsFlow.Infrastructure.Redaction;

/// <summary>
/// Stand-in redaction implementation. This is NOT a real Presidio
/// integration — it tokenizes labeled lines ("Company: X" -> "Company:
/// [REDACTED_0]") using a simple pattern, sufficient to exercise the
/// IRedactionService contract end-to-end while Ollama/Presidio tooling
/// is being set up locally. Swap this out behind the same interface once
/// real Presidio is wired — Application code does not change.
/// </summary>
public class PresidioRedactionService : IRedactionService
{
    private static readonly Regex LabeledValuePattern =
        new(@"^(?<label>[A-Za-z ]+):\s*(?<value>.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    public Task<(string RedactedText, RedactionMap Map)> RedactAsync(
        string input, CancellationToken cancellationToken = default)
    {
        var map = new Dictionary<string, string>();
        var counter = 0;

        string Redact(Match m)
        {
            var token = $"[REDACTED_{counter++}]";
            map[token] = m.Groups["value"].Value;
            return $"{m.Groups["label"].Value}: {token}";
        }

        var redactedText = LabeledValuePattern.Replace(input, Redact);
        return Task.FromResult((redactedText, new RedactionMap(map)));
    }

    public Task<string> RestoreAsync(
        string redactedText, RedactionMap map, CancellationToken cancellationToken = default)
    {
        var tokenPattern = new Regex(@"\[REDACTED_\d+\]", RegexOptions.Compiled);

        var result = tokenPattern.Replace(redactedText, m =>
        {
            if (!map.TryGetOriginal(m.Value, out var original))
                throw RedactionFailureException.RestoreMismatch(m.Value);

            return original;
        });

        return Task.FromResult(result);
    }
}
