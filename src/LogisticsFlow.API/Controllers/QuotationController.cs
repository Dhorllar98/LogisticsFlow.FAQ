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
    /// Real client credential flow - replaces the previous open dev-token
    /// placeholder. A client proves it knows its secret (checked against
    /// the BCrypt hash stored on the Client entity) before receiving a
    /// token. There is still no client onboarding/secret-rotation flow
    /// yet (out of scope for this phase) - this only verifies an
    /// already-provisioned secret, it does not issue new ones.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToken(
        [FromBody] TokenRequestDto request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (client is null || !BCrypt.Net.BCrypt.Verify(request.Secret, client.SecretHash))
        {
            // Deliberately identical response whether the account doesn't
            // exist or the secret is wrong - distinguishing the two would
            // let an attacker enumerate valid AccountIds.
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