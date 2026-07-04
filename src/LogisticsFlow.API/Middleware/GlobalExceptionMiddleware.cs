using System.Net;
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

            LlmRateLimitException ex =>
                (HttpStatusCode.TooManyRequests, "The AI provider is temporarily rate-limited. Please try again shortly."),

            LlmTimeoutException =>
                (HttpStatusCode.GatewayTimeout, "The AI provider did not respond in time."),

            LlmInvalidResponseException =>
                (HttpStatusCode.BadGateway, "The AI provider returned an unexpected response."),            
            BusinessRuleException =>
                (HttpStatusCode.UnprocessableEntity, exception.Message),

            KnowledgeBoundaryException =>
                (HttpStatusCode.ServiceUnavailable, "The knowledge base is currently unavailable. Please try again shortly."),

            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var body = JsonSerializer.Serialize(
            new { error = message, statusCode = (int)statusCode },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}