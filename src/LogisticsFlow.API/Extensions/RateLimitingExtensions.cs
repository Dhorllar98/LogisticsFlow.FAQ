using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsFlow.API.Extensions;

/// <summary>
/// Rate limiting configuration for public API endpoints. Each policy is
/// partitioned by client IP — never a single shared/global bucket — so
/// one user's traffic cannot exhaust another user's quota. See
/// docs/architecture.md "Resilience" section for the documented limit.
/// </summary>
public static class RateLimitingExtensions
{
    public const string FaqPolicy = "faq-limit";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(FaqPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
