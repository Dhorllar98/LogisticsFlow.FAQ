namespace LogisticsFlow.Infrastructure.Settings;

/// <summary>
/// Configuration shape shared across LLM provider implementations.
/// ApiKey/AnthropicVersion are unused by providers that don't need them
/// (e.g. a local Ollama instance) and are left empty in that case.
/// </summary>
public class LlmProviderSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-6";
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/messages";
    public string AnthropicVersion { get; set; } = "2023-06-01";
    public int MaxTokens { get; set; } = 1024;
}
