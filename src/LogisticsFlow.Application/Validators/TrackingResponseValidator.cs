using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

public class TrackingResponseValidator : AbstractValidator<TrackingResponseDto>
{
    public TrackingResponseValidator()
    {
        RuleFor(x => x.TrackingNumber).NotEmpty();
        RuleFor(x => x.Carrier).NotEmpty();
        RuleFor(x => x.Mode).NotEmpty();
        RuleFor(x => x.StatusSummary).NotEmpty();
    }
}