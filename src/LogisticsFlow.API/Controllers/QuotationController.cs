using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using LogisticsFlow.API.Extensions;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using LogisticsFlow.Domain.Interfaces;
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
    private readonly IClientRepository _clientRepository;
    private readonly IValidator<QuotationRequestDto> _validator;
    private readonly JwtSettings _jwtSettings;

    public QuotationController(
        IQuotationService quotationService,
        IClientRepository clientRepository,
        IValidator<QuotationRequestDto> validator,
        IOptions<JwtSettings> jwtSettings)
    {
        _quotationService = quotationService;
        _clientRepository = clientRepository;
        _validator = validator;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Lists every currently effective rate agreement for the
    /// authenticated account. Used by the frontend to present a
    /// selector when an account has more than one active agreement,
    /// before calling POST /api/quotation/quote with a chosen
    /// AgreementId.
    /// </summary>
    [HttpGet("agreements")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<RateAgreementSummaryDto>>> GetAgreements(
        CancellationToken cancellationToken)
    {
        var accountId = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized(new { error = "Token does not contain a valid account identifier." });
        }

        var agreements = await _quotationService.GetAgreementsAsync(accountId, cancellationToken);
        return Ok(agreements);
    }

    /// <summary>
    /// RESOLVED (security fix): accountId now comes exclusively from the
    /// authenticated JWT's claims, matching TrackingController's pattern.
    /// Previously QuotationRequestDto carried a client-supplied AccountId
    /// field that was never checked against the token's identity — any
    /// authenticated client could request any other client's quote by
    /// changing that field. [Authorize] alone only proves *a* valid
    /// token; it never proved the token belonged to the account being
    /// queried. That gap is closed here.
    /// </summary>
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

        var accountId = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized(new { error = "Token does not contain a valid account identifier." });
        }

        var response = await _quotationService.GetQuotationAsync(request, accountId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.TokenPolicy)]
    public async Task<IActionResult> GetToken(
        [FromBody] TokenRequestDto request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (client is null || !BCrypt.Net.BCrypt.Verify(request.Secret, client.SecretHash))
        {
            return Unauthorized(new { error = "Invalid account or secret." });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new Claim(ClaimTypes.Name, client.AccountId)
        };

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