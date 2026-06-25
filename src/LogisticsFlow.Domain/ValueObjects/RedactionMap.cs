namespace LogisticsFlow.Domain.ValueObjects;

/// <summary>
/// Shared contract type between Application (which holds it for request
/// lifetime only — never cached, logged, or persisted) and Infrastructure
/// (which produces/consumes it inside IRedactionService). Lives in Domain
/// because it is a shape both outer layers depend on; neither owns it.
///
/// Immutable by design: a redaction map must not be mutated mid-request,
/// since that would make "restore" non-deterministic.
/// </summary>
public sealed class RedactionMap
{
    private readonly Dictionary<string, string> _tokenToOriginal;

    public RedactionMap(IReadOnlyDictionary<string, string> tokenToOriginal)
    {
        _tokenToOriginal = new Dictionary<string, string>(tokenToOriginal);
    }

    public static RedactionMap Empty => new(new Dictionary<string, string>());

    public int Count => _tokenToOriginal.Count;

    public bool TryGetOriginal(string token, out string original) =>
        _tokenToOriginal.TryGetValue(token, out original!);

    public IReadOnlyDictionary<string, string> AsReadOnly() => _tokenToOriginal;
}
