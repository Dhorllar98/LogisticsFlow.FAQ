using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

/// <summary>
/// Validates incoming user requests before they reach FAQService.
/// A failure here maps to HTTP 400 — the request itself is malformed,
/// not a business or AI output problem.
/// </summary>
public class FAQRequestValidator : AbstractValidator<FAQRequestDto>
{
    public FAQRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query is required.")
            .MinimumLength(3).WithMessage("Query is too short to be meaningful.")
            .MaximumLength(500).WithMessage("Query exceeds the maximum allowed length.");

        RuleForEach(x => x.History)
            .ChildRules(history =>
            {
                history.RuleFor(m => m.Content)
                    .NotEmpty().WithMessage("History entries cannot have empty content.");
            })
            .When(x => x.History is not null);
    }
}