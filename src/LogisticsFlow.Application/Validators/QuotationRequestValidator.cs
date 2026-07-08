using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

/// <summary>
/// Validates incoming quotation requests before they reach the service
/// layer. AccountId validation was removed along with the AccountId
/// field itself — see QuotationRequestDto for why.
/// </summary>
public class QuotationRequestValidator : AbstractValidator<QuotationRequestDto>
{
    private const int MaxQueryLength = 500;

    public QuotationRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

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