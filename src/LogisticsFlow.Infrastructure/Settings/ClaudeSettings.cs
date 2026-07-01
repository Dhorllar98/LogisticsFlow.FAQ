namespace LogisticsFlow.Infrastructure.Settings;

public class ClaudeSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string AnthropicVersion { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1024;
}