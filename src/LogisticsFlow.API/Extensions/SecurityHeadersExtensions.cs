namespace LogisticsFlow.API.Extensions;

/// <summary>
/// Adds baseline HTTP security response headers to every response.
/// Registered as the first middleware after GlobalExceptionMiddleware,
/// so headers are present on every response - including error
/// responses and the Scalar/OpenAPI surface - not just successful
/// controller results.
///
/// Closes the HTTP security headers gap flagged since Phase 3's
/// original tech-debt review and carried forward, unresolved, through
/// Phase 3.5 (see docs/security-hardening-checklist.md, Section 4).
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] =
                "geolocation=(), microphone=(), camera=()";

            await next();
        });

        return app;
    }
}