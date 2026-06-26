using FluentValidation.TestHelper;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Validators;
using Xunit;

namespace LogisticsFlow.Application.Tests.Validators;

public class QuotationResponseValidatorTests
{
    private readonly QuotationResponseValidator _validator = new();

    private static QuotationResponseDto ValidResponse() => new()
    {
        ClientId = Guid.NewGuid(),
        NegotiatedRate = 1200.50m,
        OriginAddress = "123 Dock Rd, Lagos",
        DestinationAddress = "45 Port Ave, Apapa",
        ComposedMessage = "Your quote is ready."
    };

    [Fact]
    public void Validate_ValidResponse_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(ValidResponse());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyClientId_ShouldHaveError()
    {
        var dto = ValidResponse();
        dto.ClientId = Guid.Empty;

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Fact]
    public void Validate_NonPositiveRate_ShouldHaveError()
    {
        var dto = ValidResponse();
        dto.NegotiatedRate = 0;

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NegotiatedRate);
    }

    [Fact]
    public void Validate_EmptyComposedMessage_ShouldHaveError()
    {
        var dto = ValidResponse();
        dto.ComposedMessage = "";

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ComposedMessage);
    }
}
