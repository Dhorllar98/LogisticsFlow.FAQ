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
[EnableRateLimiting(RateLimitingExtensions.RiskAssessmentPolicy)]
public class RiskAssessmentController : ControllerBase
{
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly IValidator<RiskAssessmentRequestDto> _validator;

    public RiskAssessmentController(
        IRiskAssessmentService riskAssessmentService,
        IValidator<RiskAssessmentRequestDto> validator)
    {
        _riskAssessmentService = riskAssessmentService;
        _validator = validator;
    }

    /// <summary>
    /// Returns a deterministic risk level plus an AI-composed suggested
    /// action for a shipment, scoped to the authenticated client's
    /// account. Risk level is computed in RiskAssessmentService from
    /// aggregate, depersonalized lane-history statistics - never from
    /// another client's individual shipment data. A tracking number that
    /// exists but belongs to a different account returns 404,
    /// identically to a tracking number that does not exist at all.
    /// </summary>
    [HttpPost("risk-assessment")]
    [Authorize]
    public async Task<ActionResult<RiskAssessmentResponseDto>> GetRiskAssessment(
        [FromBody] RiskAssessmentRequestDto request,
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

        var response = await _riskAssessmentService.AssessAsync(request, accountId, cancellationToken);
        return Ok(response);
    }
}