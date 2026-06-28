using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace LogisticsFlow.Infrastructure.AI;

public class ClaudeApiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderSettings _settings;
    private readonly ILogger<ClaudeApiClient> _logger;

    public ClaudeApiClient(HttpClient httpClient, IOptions<LlmProviderSettings> settings, ILogger<ClaudeApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new ClaudeRequest(
            Model: _settings.Model,
            MaxTokens: _settings.MaxTokens,
            System: systemPrompt,
            Messages: conversationHistory
                .Select(m => new ClaudeMessage(m.Role == ChatRole.User ? "user" : "assistant", m.Content))
                .ToList());

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl)
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Add("x-api-key", _settings.ApiKey);
        request.Headers.Add("anthropic-version", _settings.AnthropicVersion);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Claude API call timed out.");
            throw new LlmTimeoutException("The AI provider did not respond in time.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta;
                _logger.LogWarning("Claude API rate limit hit. RetryAfter={RetryAfter}", retryAfter);
                throw new LlmRateLimitException("The AI provider rate-limited this request.", retryAfter);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Claude API call failed with status {StatusCode}: {ErrorBody}", (int)response.StatusCode, errorBody);
                throw new LlmInvalidResponseException($"The AI provider returned an unexpected status: {(int)response.StatusCode}.");
            }

            var parsed = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: cancellationToken);
            var textBlock = parsed?.Content?.FirstOrDefault(c => c.Type == "text");

            if (textBlock is null || string.IsNullOrWhiteSpace(textBlock.Text))
                throw new LlmInvalidResponseException("Claude API returned a response with no text content.");

            return textBlock.Text;
        }
    }

    private sealed record ClaudeRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] List<ClaudeMessage> Messages);

    private sealed record ClaudeMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ClaudeResponse(
        [property: JsonPropertyName("content")] List<ClaudeContentBlock>? Content);

    private sealed record ClaudeContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
