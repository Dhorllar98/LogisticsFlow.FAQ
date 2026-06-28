using LogisticsFlow.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LogisticsFlow.Integration.Tests;

/// <summary>
/// Test-only WebApplicationFactory. Program.cs reads JwtSettings directly
/// from configuration at startup and bakes the signing key into
/// JwtBearerOptions.TokenValidationParameters before the host is built -
/// bypassing the IOptions pipeline entirely for *validation*. PostConfigure
/// on JwtSettings alone only affects components that consume IOptions
/// JwtSettings) directly, like the token-issuing controller). Both must
/// be overridden, or issued tokens are signed with one key and validated
/// against another.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "test-only-signing-key-not-for-production-use-32-bytes-min";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtSettings>(options =>
            {
                options.SigningKey = TestSigningKey;
            });

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
            });
        });
    }
}
