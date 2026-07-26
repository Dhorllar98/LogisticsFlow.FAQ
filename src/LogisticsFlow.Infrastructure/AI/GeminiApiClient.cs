using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogisticsFlow.Infrastructure.AI;

/// <summary>
/// Gemini implementation of ILlmClient, added for manual side-by-side
/// provider comparison (ActiveProvider switch), not automatic failover.
/// Mirrors ClaudeApiClient's resilience posture (explicit timeout,
/// 429/error handling, structured logging) so both providers are held
/// to the same production-hardening standard rather than one being a
/// shortcut version of the other.
///
/// Gemini's request shape differs structurally from Claude's: there is
/// no separate "system" field - system instructions go in a
/// "systemInstruction" object, and roles are "user"/"model", not
/// "user"/"assistant". The API key is passed as a query parameter, not
/// a header, per Gemini's REST API convention.
/// </summary>
public class GeminiApiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiApiClient> _logger;

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public GeminiApiClient(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiApiClient> logger)
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
        var requestBody = new GeminiRequest(
            SystemInstruction: new GeminiSystemInstruction(
                Parts: new List<GeminiPart> { new(systemPrompt) }),
            Contents: conversationHistory
                .Select(m => new GeminiContent(
                    Role: m.Role == ChatRole.User ? "user" : "model",
                    Parts: new List<GeminiPart> { new(m.Content) }))
                .ToList(),
            GenerationConfig: new GeminiGenerationConfig(MaxOutputTokens: _settings.MaxTokens));

        var requestUri = $"{_settings.BaseUrl.TrimEnd('/')}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(requestBody)
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, timeoutCts.Token);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Gemini API call timed out after {TimeoutSeconds}s.", RequestTimeout.TotalSeconds);
            throw new LlmTimeoutException("The AI provider did not respond in time.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta;
                _logger.LogWarning("Gemini API rate limit hit. RetryAfter={RetryAfter}", retryAfter);
                throw new LlmRateLimitException("The AI provider rate-limited this request.", retryAfter);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Gemini API call failed with status {StatusCode}: {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody);
                throw new LlmInvalidResponseException(
                    $"The AI provider returned an unexpected status: {(int)response.StatusCode}.");
            }

            var parsed = await response.Content
                .ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text))
                throw new LlmInvalidResponseException(
                    "Gemini API returned a response with no text content.");

            return text;
        }
    }

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiSystemInstruction SystemInstruction,
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiSystemInstruction(
        [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiResponseContent? Content);

    private sealed record GeminiResponseContent(
        [property: JsonPropertyName("parts")] List<GeminiResponsePart>? Parts);

    private sealed record GeminiResponsePart(
        [property: JsonPropertyName("text")] string? Text);
}