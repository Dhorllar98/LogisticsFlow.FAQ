using FluentValidation;
using LogisticsFlow.API.Extensions;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogisticsFlow.API.Controllers;

// MODIFIED FROM YOUR REAL PHASE 1 FILE - only the [EnableRateLimiting]
// attribute and its using statements were added, to resolve the flagged
// "Quotation silently inherits FAQ's rate limit policy" issue. Nothing
// else in this controller changed from what you pasted.
[ApiController]
[Route("api/faq")]
[EnableRateLimiting(RateLimitingExtensions.FaqPolicy)]
public class FAQController : ControllerBase
{
    private readonly IFAQService _faqService;
    private readonly IValidator<FAQRequestDto> _validator;

    public FAQController(IFAQService faqService, IValidator<FAQRequestDto> validator)
    {
        _faqService = faqService;
        _validator = validator;
    }

    /// <summary>
    /// Submits a logistics question to the grounded FAQ assistant.
    /// Returns a structured response including confidence score,
    /// grounding sources, and an escalation flag when the AI cannot
    /// answer from the knowledge base.
    /// </summary>
    [HttpPost("ask")]
    public async Task<ActionResult<FAQResponseDto>> Ask(
        [FromBody] FAQRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors
                .Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));
        }

        var response = await _faqService.AskAsync(request, cancellationToken);
        return Ok(response);
    }
}
