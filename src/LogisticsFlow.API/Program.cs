using LogisticsFlow.API.Extensions;
using LogisticsFlow.API.Middleware;
using LogisticsFlow.Application;
using LogisticsFlow.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Layer wiring ──────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── ASP.NET Core services ─────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// ── Cross-cutting concerns ────────────────────────────────────────────────
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApiRateLimiting();

// ── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting(RateLimitingExtensions.FaqPolicy);

app.Run();