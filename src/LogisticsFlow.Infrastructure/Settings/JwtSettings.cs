namespace LogisticsFlow.Infrastructure.Settings;

public class JwtSettings
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LogisticsFlow";
    public string Audience { get; set; } = "LogisticsFlow.Clients";
    public int AccessTokenExpiryMinutes { get; set; } = 15;
}