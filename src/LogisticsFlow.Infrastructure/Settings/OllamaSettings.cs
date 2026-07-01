namespace LogisticsFlow.Infrastructure.Settings;

public class OllamaSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1024;
}