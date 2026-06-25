using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsFlow.API.Extensions;

/// <summary>
/// Rate limiting configuration for public API endpoints. Each policy is
/// partitioned by client IP - never a single shared/global bucket - so
/// one user's traffic cannot exhaust another user's quota.
///
/// RESOLVED (was flagged): FAQ and Quotation now have separate named
/// policies, applied explicitly per-controller via [EnableRateLimiting]
/// rather than inherited as a side effect of a single global
/// .RequireRateLimiting() call on MapControllers(). CLAUDE.md only
/// specified a limit for /api/faq/ask; QuotationPolicy below uses the
/// same 20/min default as a starting point since no distinct limit was
/// ever specified for Quotation - revisit if Quotation's cost profile
/// (DB lookup + redact + Claude call) warrants a tighter limit than
/// FAQ's cache-assisted flow.
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

    /// <summary>
    /// RESOLVED (was flagged, found during integration test review): this
    /// previously read httpContext.Connection.RemoteIpAddress directly,
    /// which is the proxy's IP - not the real client's - once deployed
    /// behind Railway/Render's reverse proxy (see docs/deployment.md).
    /// That would have made the per-IP limiter effectively global in
    /// production, the exact bug already fixed once in Phase 1 for a
    /// different reason. X-Forwarded-For is checked first, with the
    /// direct connection IP as a local-dev fallback.
    /// </summary>
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
