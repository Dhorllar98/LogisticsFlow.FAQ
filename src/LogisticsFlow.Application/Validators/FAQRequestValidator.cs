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
    private const int MaxHistoryEntries = 6;

    public FAQRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Query is required.")
            .MinimumLength(3).WithMessage("Query is too short to be meaningful.")
            .MaximumLength(500).WithMessage("Query exceeds the maximum allowed length.")
            .Must(q => q.Any(char.IsLetter))
            .WithMessage("Query must contain readable text.");

        RuleFor(x => x.History)
            .Must(h => h == null || h.Count <= MaxHistoryEntries)
            .WithMessage($"History cannot exceed {MaxHistoryEntries} entries.");

        RuleForEach(x => x.History)
            .ChildRules(history =>
            {
                history.RuleFor(m => m.Content)
                    .NotEmpty().WithMessage("History entries cannot have empty content.")
                    .MaximumLength(500).WithMessage("History entry content exceeds the maximum allowed length.");
            })
            .When(x => x.History is not null);
    }
}