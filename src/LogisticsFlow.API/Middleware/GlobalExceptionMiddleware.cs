using System.Net;
using System.Net.Http;
using System.Text.Json;
using LogisticsFlow.Domain.Exceptions;

namespace LogisticsFlow.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // IMPORTANT: order matters. C# type-pattern switches match a base
        // type against derived instances, so any exception needing a
        // status code OTHER than BusinessRuleException's default 422 must
        // be listed before the generic BusinessRuleException case, or it
        // will silently match that case first.
        var (statusCode, message) = exception switch
        {
            RateAgreementNotFoundException =>
                (HttpStatusCode.NotFound, exception.Message),

            TrackingNotFoundException =>
                (HttpStatusCode.NotFound, exception.Message),

            RedactionFailureException =>
                (HttpStatusCode.UnprocessableEntity, "Request could not be processed safely."),

            LlmRateLimitException =>
                (HttpStatusCode.TooManyRequests, "The AI provider is temporarily rate-limited. Please try again shortly."),

            LlmTimeoutException =>
                (HttpStatusCode.GatewayTimeout, "The AI provider did not respond in time."),

            LlmInvalidResponseException =>
                (HttpStatusCode.BadGateway, "The AI provider returned an unexpected response."),

            // RESOLVED: a raw connection-level failure (e.g. a dropped
            // network mid-request) surfaces as HttpRequestException from
            // HttpClient itself, before it ever reaches one of our typed
            // LlmProviderException subtypes. Previously fell through to
            // the generic 500 catch-all - confirmed during live Gemini/
            // Claude comparison testing when a transient network drop
            // produced two consecutive unmapped 500s. Mapped to 502 to
            // match LlmInvalidResponseException's semantics: the upstream
            // provider connection itself failed, not our own logic.
            HttpRequestException =>
                (HttpStatusCode.BadGateway, "Could not reach the AI provider. Please try again shortly."),

            BusinessRuleException =>
                (HttpStatusCode.UnprocessableEntity, exception.Message),

            KnowledgeBoundaryException =>
                (HttpStatusCode.ServiceUnavailable, "The knowledge base is currently unavailable. Please try again shortly."),

            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.ContentType = "application/json";
        if (exception is LlmRateLimitException rateLimitEx && rateLimitEx.RetryAfter.HasValue)
        {
            context.Response.Headers.RetryAfter = 
            ((int)rateLimitEx.RetryAfter.Value.TotalSeconds).ToString();
        }
        context.Response.StatusCode = (int)statusCode;

        var body = JsonSerializer.Serialize(
            new { error = message, statusCode = (int)statusCode },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}