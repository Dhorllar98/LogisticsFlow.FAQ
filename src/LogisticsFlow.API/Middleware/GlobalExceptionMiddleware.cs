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
        var (statusCode, message) = exception switch
        {
            BusinessRuleException => 
                (HttpStatusCode.UnprocessableEntity, exception.Message),
            KnowledgeBoundaryException => 
                (HttpStatusCode.ServiceUnavailable, "The knowledge base is currently unavailable. Please try again shortly."),
            _ => 
                (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Stack traces never reach the client — logged internally above only
       var body = JsonSerializer.Serialize(
    new { error = message, statusCode = (int)statusCode },
    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}