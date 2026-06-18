using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace LogisticsFlow.Infrastructure.AI;

public class ClaudeApiClient : IClaudeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeApiSettings _settings;

    public ClaudeApiClient(HttpClient httpClient, IOptions<ClaudeApiSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
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

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BusinessRuleException($"Claude API call failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: cancellationToken);
        var textBlock = parsed?.Content?.FirstOrDefault(c => c.Type == "text");

        if (textBlock is null || string.IsNullOrWhiteSpace(textBlock.Text))
            throw new BusinessRuleException("Claude API returned a response with no text content.");

        return textBlock.Text;
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