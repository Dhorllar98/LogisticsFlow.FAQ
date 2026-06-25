using LogisticsFlow.API.Extensions;
using LogisticsFlow.API.Middleware;
using LogisticsFlow.Application;
using LogisticsFlow.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// -- Layer wiring ------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// -- ASP.NET Core services ---------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// -- Cross-cutting concerns ---------------------------------------------------
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApiRateLimiting();

// -- Build --------------------------------------------------------------------
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();

// RESOLVED (was flagged): no longer applying a single rate limit policy
// globally here. FAQController and QuotationController each declare
// their own policy explicitly via [EnableRateLimiting(...)] - see those
// files. This was previously a side effect, not a decision; it's now
// explicit per-endpoint, matching the rest of this codebase's "flag
// before coding, never silently inherit" standard.
app.MapControllers();

app.Run();

// Required so WebApplicationFactory<Program> in the integration test
// project can see this entry point. Top-level statements generate an
// internal Program class by default; this partial declaration makes it
// public without changing any runtime behavior above.
public partial class Program { }
