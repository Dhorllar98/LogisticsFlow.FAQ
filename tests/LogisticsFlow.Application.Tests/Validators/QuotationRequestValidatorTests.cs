using FluentValidation.TestHelper;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Validators;
using Xunit;

namespace LogisticsFlow.Application.Tests.Validators;

public class QuotationRequestValidatorTests
{
    private readonly QuotationRequestValidator _validator = new();

    [Fact]
    public void Validate_NoCustomerQuery_ShouldNotHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto());
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerQuery);
    }

    [Fact]
    public void Validate_CustomerQueryTooLong_ShouldHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto
        {
            CustomerQuery = new string('a', 501)
        });

        result.ShouldHaveValidationErrorFor(x => x.CustomerQuery);
    }

    [Fact]
    public void Validate_CustomerQueryWithNoAlphabeticContent_ShouldHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto
        {
            CustomerQuery = "12345 !!! ###"
        });

        result.ShouldHaveValidationErrorFor(x => x.CustomerQuery);
    }

    [Fact]
    public void Validate_ValidCustomerQuery_ShouldNotHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto
        {
            CustomerQuery = "Can you confirm the handling instructions?"
        });

        result.ShouldNotHaveValidationErrorFor(x => x.CustomerQuery);
    }
}