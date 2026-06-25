using FluentValidation;
using LogisticsFlow.API.Extensions;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsFlow.API.Controllers;

[ApiController]
[Route("api/quotation")]
[EnableRateLimiting(RateLimitingExtensions.QuotationPolicy)]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _quotationService;
    private readonly IValidator<QuotationRequestDto> _validator;

    public QuotationController(IQuotationService quotationService, IValidator<QuotationRequestDto> validator)
    {
        _quotationService = quotationService;
        _validator = validator;
    }

    /// <summary>
    /// Looks up the caller's current rate agreement and returns a
    /// Claude-composed quotation summary. RateAgreementNotFoundException
    /// and RedactionFailureException are handled centrally by
    /// GlobalExceptionMiddleware (404 and 422 respectively) - this
    /// controller does not catch anything itself.
    /// </summary>
    [HttpPost("quote")]
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
}
