namespace LogisticsFlow.Domain.Exceptions;

/// <summary>
/// Base type for failures originating from any LLM provider integration
/// (Claude, Ollama, or future providers). Application catches this type
/// when it doesn't need to distinguish the failure cause; it catches a
/// specific subtype when the failure mode changes the business decision
/// (e.g. retry vs. don't retry, escalate vs. fail).
/// </summary>
public abstract class LlmProviderException : Exception
{
    protected LlmProviderException(string message) : base(message) { }
    protected LlmProviderException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// The provider call exceeded its configured timeout. Distinct from
/// LlmInvalidResponseException because a timeout is retry-eligible;
/// a malformed response usually is not.
/// </summary>
public sealed class LlmTimeoutException : LlmProviderException
{
    public LlmTimeoutException(string message) : base(message) { }
    public LlmTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// The provider rejected the call with a rate-limit response (e.g. HTTP 429).
/// Carries RetryAfter when the provider supplies it, so Application/Polly
/// can honor it instead of guessing a backoff.
/// </summary>
public sealed class LlmRateLimitException : LlmProviderException
{
    public TimeSpan? RetryAfter { get; }

    public LlmRateLimitException(string message, TimeSpan? retryAfter = null) : base(message)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// The provider responded successfully at the transport level, but the
/// payload was missing, empty, or failed to parse into the expected shape
/// (e.g. Phase 1/2's markdown-fence-wrapped JSON bug). Not retry-eligible
/// on its own.
/// </summary>
public sealed class LlmInvalidResponseException : LlmProviderException
{
    public LlmInvalidResponseException(string message) : base(message) { }
}
