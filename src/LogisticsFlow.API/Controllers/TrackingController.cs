using System.Security.Claims;
using FluentValidation;
using LogisticsFlow.API.Extensions;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsFlow.API.Controllers;

[ApiController]
[Route("api/tracking")]
[EnableRateLimiting(RateLimitingExtensions.TrackingPolicy)]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly IValidator<TrackingRequestDto> _validator;

    public TrackingController(
        ITrackingService trackingService,
        IValidator<TrackingRequestDto> validator)
    {
        _trackingService = trackingService;
        _validator = validator;
    }

    /// <summary>
    /// Returns an AI-composed status summary for a shipment, scoped to
    /// the authenticated client's account. A tracking number that exists
    /// but belongs to a different account returns 404, identically to a
    /// tracking number that does not exist at all — see
    /// ITrackingRepository.GetByTrackingNumberForAccountAsync.
    /// </summary>
    [HttpPost("status")]
    [Authorize]
    public async Task<ActionResult<TrackingResponseDto>> GetStatus(
        [FromBody] TrackingRequestDto request,
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
            // Defensive only — [Authorize] should already guarantee a
            // valid token with this claim, since QuotationController's
            // /token endpoint always sets it. A missing claim here means
            // a token was issued outside that flow.
            return Unauthorized(new { error = "Token does not contain a valid account identifier." });
        }

        var response = await _trackingService.GetStatusAsync(request, accountId, cancellationToken);
        return Ok(response);
    }
}