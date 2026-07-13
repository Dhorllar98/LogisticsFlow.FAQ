using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

public class RiskAssessmentResponseValidator : AbstractValidator<RiskAssessmentResponseDto>
{
    public RiskAssessmentResponseValidator()
    {
        RuleFor(x => x.TrackingNumber).NotEmpty();
        RuleFor(x => x.Carrier).NotEmpty();
        RuleFor(x => x.RiskLevel).NotEmpty();
        RuleFor(x => x.SuggestedAction).NotEmpty();
    }
}