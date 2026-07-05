using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

/// <summary>
/// Validates incoming quotation requests before they reach the service
/// layer. Per security-hardening-checklist.md Section 1: explicit length
/// ceilings on free-text fields, allow-list/format checks on structured
/// fields, reject (400) rather than silently coerce.
/// </summary>
public class QuotationRequestValidator : AbstractValidator<QuotationRequestDto>
{
    private const int MaxQueryLength = 500;

    public QuotationRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("AccountId is required.")
            .MaximumLength(64)
            .WithMessage("AccountId exceeds maximum allowed length.");

        RuleFor(x => x.CustomerQuery)
            .MaximumLength(MaxQueryLength)
            .WithMessage($"CustomerQuery cannot exceed {MaxQueryLength} characters.")
            .Must(ContainAlphabeticContent)
            .WithMessage("CustomerQuery must contain readable text.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerQuery));
    }

    private static bool ContainAlphabeticContent(string? query) =>
        string.IsNullOrWhiteSpace(query) || query.Any(char.IsLetter);
}
