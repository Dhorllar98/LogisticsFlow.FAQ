using LogisticsFlow.API.Extensions;
using LogisticsFlow.API.Middleware;
using LogisticsFlow.Application;
using LogisticsFlow.Infrastructure;
using LogisticsFlow.Infrastructure.Persistence;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtSecuritySchemeTransformer>();
});

builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApiRateLimiting();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey))
        };

        // RESOLVED: JwtBearerHandler short-circuits the pipeline on auth
        // failure BEFORE GlobalExceptionMiddleware or any controller runs,
        // so the default behavior (empty 401 body) never picks up this
        // API's standard { "error": ..., "statusCode": ... } contract.
        // Wiring it here is the only place that can intercept it.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                var body = JsonSerializer.Serialize(
                    new { error = "Missing or invalid authentication token.", statusCode = 401 },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await context.Response.WriteAsync(body);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-migrate on startup in Production only (Railway deployment).
// Development: migrations are run manually via dotnet ef.
// Tests: never run migrations — they do not exercise the database layer
// and the local SQL Server connection string is invalid for Npgsql.

//Commenting out this part for now because i'm skipping DB.
//if (app.Environment.IsProduction())
//{
   // using var scope = app.Services.CreateScope();
    //var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //db.Database.Migrate();
//}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter(); 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }