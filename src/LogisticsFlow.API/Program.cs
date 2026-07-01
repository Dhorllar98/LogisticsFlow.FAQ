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