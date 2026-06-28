using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogisticsFlow.Infrastructure.AI;

/// <summary>
/// Stub implementation of ILlmClient for a local Ollama/Llama instance.
/// Wire format is NOT yet implemented. Throws deliberately so DI cannot
/// silently resolve this as ActiveProvider until it's filled in.
/// </summary>
public class OllamaApiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderSettings _settings;
    private readonly ILogger<OllamaApiClient> _logger;

    public OllamaApiClient(HttpClient httpClient, IOptions<LlmProviderSettings> settings, ILogger<OllamaApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("OllamaApiClient.SendMessageAsync called but wire format is not yet implemented.");
        throw new LlmInvalidResponseException(
            "OllamaApiClient is a Phase 2.5 stub. Wire format implementation is deferred until Tier 3 routing is built.");
    }
}
