using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsFlow.API.Extensions;

/// <summary>
/// Rate limiting configuration for public API endpoints. Each policy is
/// partitioned by client IP - never a single shared/global bucket - so
/// one user's traffic cannot exhaust another user's quota.
/// </summary>
public static class RateLimitingExtensions
{
    public const string FaqPolicy = "faq-limit";
    public const string QuotationPolicy = "quotation-limit";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // RESOLVED (Finding D, found via load test): ASP.NET Core's
            // rate limiter does NOT set Retry-After automatically on
            // rejection - it has to be read from the lease metadata and
            // written explicitly here, or the 429 response goes out with
            // no Retry-After header at all, which is what the load test
            // caught. Also writes a small JSON body so a rejected request
            // doesn't return an empty 429 with no explanation.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"error":"Rate limit exceeded. Please try again shortly."}""",
                    cancellationToken);
            };

            options.AddPolicy(FaqPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveClientIp(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy(QuotationPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolveClientIp(httpContext),
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

    private static string ResolveClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}