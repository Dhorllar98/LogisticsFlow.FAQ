using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

public class RiskAssessmentRequestValidator : AbstractValidator<RiskAssessmentRequestDto>
{
    public RiskAssessmentRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Za-z0-9\-]+$")
            .WithMessage("Tracking number contains invalid characters.");
    }
}