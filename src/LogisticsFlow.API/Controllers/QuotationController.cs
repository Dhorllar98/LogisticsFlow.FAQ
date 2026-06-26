using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using LogisticsFlow.API.Extensions;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using LogisticsFlow.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LogisticsFlow.API.Controllers;

[ApiController]
[Route("api/quotation")]
[EnableRateLimiting(RateLimitingExtensions.QuotationPolicy)]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _quotationService;
    private readonly IValidator<QuotationRequestDto> _validator;
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;

    public QuotationController(
        IQuotationService quotationService,
        IValidator<QuotationRequestDto> validator,
        IOptions<JwtSettings> jwtSettings,
        IWebHostEnvironment environment)
    {
        _quotationService = quotationService;
        _validator = validator;
        _jwtSettings = jwtSettings.Value;
        _environment = environment;
    }

    [HttpPost("quote")]
    [Authorize]
    public async Task<ActionResult<QuotationResponseDto>> GetQuote(
        [FromBody] QuotationRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors
                .Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));
        }

        var response = await _quotationService.GetQuotationAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// DEV-ONLY token issuance for manual smoke testing. There is no real
    /// login/credential flow yet - this exists solely so the auth gap can
    /// be closed and smoke-tested this session without scope-creeping into
    /// building a full user/login system, which belongs to a later phase.
    /// MUST be removed or properly gated before any real deployment.
    /// </summary>
    [HttpPost("dev-token")]
    [AllowAnonymous]
    public IActionResult GetDevToken()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "dev-smoke-test-user") };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}