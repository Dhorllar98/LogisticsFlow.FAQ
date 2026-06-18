using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using LogisticsFlow.Application;
using LogisticsFlow.Infrastructure;
using LogisticsFlow.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Layer wiring ──────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── ASP.NET Core services ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── CORS ──────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("LogisticsFlowPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Rate limiting: 20 requests per IP per minute ──────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("faq-limit", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

// ── Build ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("LogisticsFlowPolicy");
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("faq-limit");

app.Run();