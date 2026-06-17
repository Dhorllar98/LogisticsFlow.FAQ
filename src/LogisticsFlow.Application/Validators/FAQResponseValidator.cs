using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

/// <summary>
/// Validates the AI-derived response before it leaves the Application
/// layer. A failure here means the AI's output could not be trusted into
/// a safe shape — this maps to HTTP 422, never 400, since the original
/// client request was perfectly valid.
/// </summary>
public class FAQResponseValidator : AbstractValidator<FAQResponseDto>
{
    private const double EscalationThreshold = 0.70;

    public FAQResponseValidator()
    {
        RuleFor(x => x.Answer)
            .NotEmpty().WithMessage("AI response content is required.")
            .MinimumLength(10).WithMessage("AI response is too short to be useful.");

        RuleFor(x => x.ConfidenceScore)
            .InclusiveBetween(0.0, 1.0)
            .WithMessage("Confidence score must be between 0.0 and 1.0.");

        RuleFor(x => x.GroundingSources)
            .NotNull().WithMessage("Grounding sources list cannot be null.");

        RuleFor(x => x.EscalationBoolean)
            .Equal(true)
            .When(x => x.ConfidenceScore < EscalationThreshold || x.GroundingSources.Count == 0)
            .WithMessage("Low-confidence or ungrounded responses must be flagged for escalation.");
    }
}