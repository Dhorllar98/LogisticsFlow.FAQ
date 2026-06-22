namespace LogisticsFlow.API.Extensions;

/// <summary>
/// CORS policy configuration. Origins are read from configuration
/// (AllowedOrigins) — never wildcarded — per the governing security
/// standards (S-Tier skill, Section VII).
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "LogisticsFlowPolicy";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173" };

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }
}
