using FluentValidation;
using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Validators;

/// <summary>
/// Schema-validates the shaped response before it leaves the service
/// layer. Per security-hardening-checklist.md Section 2: every AI-derived
/// response is schema-validated; malformed output is a system failure
/// (422), never silently passed through.
/// </summary>
public class QuotationResponseValidator : AbstractValidator<QuotationResponseDto>
{
    public QuotationResponseValidator()
    {
        RuleFor(x => x.ClientId).NotEqual(Guid.Empty);
        RuleFor(x => x.NegotiatedRate).GreaterThan(0);
        RuleFor(x => x.OriginAddress).NotEmpty();
        RuleFor(x => x.DestinationAddress).NotEmpty();
        RuleFor(x => x.ComposedMessage).NotEmpty();
    }
}
