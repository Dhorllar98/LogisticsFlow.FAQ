using System.Net;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogisticsFlow.Integration.Tests;

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
        var client = services.GetRequiredService<ILlmClient>();

        var result = await client.SendMessageAsync(
            "system prompt", new List<ChatMessage> { new() { Role = ChatRole.User, Content = "hi" } });

        Assert.Contains("ok", result);
        Assert.True(handler.CallCount >= 3, "Expected at least 2 retries before success on a 5xx.");
    }

    /// <summary>
    /// RESOLVED (Finding B): this test originally documented the OLD
    /// broken behavior (no retry on 429, CallCount == 1). After fixing
    /// Infrastructure/DependencyInjection.cs to extend the retry
    /// predicate to include 429, a real retry now happens - this test
    /// was stale and asserting the bug as if it were correct behavior.
    /// Updated to assert the FIXED behavior: at least one retry occurs.
    /// </summary>
    [Fact]
    public async Task SendMessageAsync_429_NowRetriesUnderFixedPolicy()
    {
        var handler = new FakeHttpMessageHandler(new[]
        {
            new HttpResponseMessage((HttpStatusCode)429),
            new HttpResponseMessage((HttpStatusCode)429)
        });

        var services = TestServiceProviderFactory.BuildWithFakeHandler(handler);
        var client = services.GetRequiredService<ILlmClient>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendMessageAsync("system prompt", new List<ChatMessage> { new() { Role = ChatRole.User, Content = "hi" } }));

        Assert.True(handler.CallCount >= 2, "Expected at least one retry on a 429 after the fix - got only 1 call.");
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
        var client = services.GetRequiredService<ILlmClient>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.SendMessageAsync("system prompt", new List<ChatMessage> { new() { Role = ChatRole.User, Content = "hi" } }));
    }
}
