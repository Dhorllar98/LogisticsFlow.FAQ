using System.Net;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

/// <summary>
/// Exercises the REAL typed HttpClient (AddStandardResilienceHandler) via
/// a fake message handler, not a service-level mock — the point is to
/// verify the actual resilience pipeline's behavior, which a mocked
/// IClaudeApiClient would bypass entirely.
/// </summary>
public class ClaudeApiClientResilienceTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public int CallCount { get; private set; }

        public FakeHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
                throw new InvalidOperationException("No more fake responses queued.");

            return Task.FromResult(_responses.Dequeue());
        }
    }

    [Fact]
    public async Task SendMessageAsync_TransientServerErrorThenSuccess_RetriesAndEventuallySucceeds()
    {
        // 503 is in AddStandardResilienceHandler's default transient set
        // (5xx/408), so this SHOULD be retried automatically.
        var handler = new FakeHttpMessageHandler(new[]
        {
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"content":[{"type":"text","text":"ok"}]}""")
            }
        });

        var services = TestServiceProviderFactory.BuildWithFakeHandler(handler);
        var client = services.GetRequiredService<IClaudeApiClient>();

        var result = await client.SendMessageAsync(
            "system prompt", new List<ChatMessage> { new() { Role = ChatRole.User, Content = "hi" } });

        Assert.Contains("ok", result);
        Assert.True(handler.CallCount >= 3, "Expected at least 2 retries before success on a 5xx.");
    }

    /// <summary>
    /// IMPORTANT FINDING, not a guess: AddStandardResilienceHandler's
    /// default transient classifier covers 5xx and 408 only — NOT 429.
    /// This test documents that REAL current behavior (immediate failure
    /// on the first 429, no retry) rather than the retry behavior I
    /// originally assumed before seeing your actual Infrastructure code.
    /// If you want 429 retried/failed-over per CLAUDE.md's "fallback on
    /// 429 or 5xx" language, that needs an explicit custom resilience
    /// pipeline — see the note left in Infrastructure/DependencyInjection.cs.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_429_DoesNotRetryUnderCurrentDefaultPolicy_FailsOnFirstAttempt()
    {
        var handler = new FakeHttpMessageHandler(new[]
        {
            new HttpResponseMessage((HttpStatusCode)429)
        });

        var services = TestServiceProviderFactory.BuildWithFakeHandler(handler);
        var client = services.GetRequiredService<IClaudeApiClient>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendMessageAsync("system prompt", new List<ChatMessage> { new() { Role = ChatRole.User, Content = "hi" } }));

        Assert.Equal(1, handler.CallCount); // No retry occurred — documents current real behavior.
    }

    [Fact]
    public async Task SendMessageAsync_MalformedJsonResponse_ThrowsRatherThanReturningGarbage()
    {
        var handler = new FakeHttpMessageHandler(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not valid json {{{")
            }
        });

        var services = TestServiceProviderFactory.BuildWithFakeHandler(handler);
        var client = services.GetRequiredService<IClaudeApiClient>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendMessageAsync("system prompt", new List<ChatMessage> { new() { Role = ChatRole.User, Content = "hi" } }));
    }
}
