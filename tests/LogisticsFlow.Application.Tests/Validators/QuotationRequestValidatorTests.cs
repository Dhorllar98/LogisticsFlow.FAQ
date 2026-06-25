using FluentValidation.TestHelper;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Validators;
using Xunit;

namespace LogisticsFlow.Application.Tests.Validators;

public class QuotationRequestValidatorTests
{
    private readonly QuotationRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyAccountId_ShouldHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto { AccountId = "" });
        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_AccountIdTooLong_ShouldHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto { AccountId = new string('a', 65) });
        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_ValidAccountId_NoCustomerQuery_ShouldNotHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto { AccountId = "ACC-123" });
        result.ShouldNotHaveValidationErrorFor(x => x.AccountId);
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerQuery);
    }

    [Fact]
    public void Validate_CustomerQueryTooLong_ShouldHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto
        {
            AccountId = "ACC-123",
            CustomerQuery = new string('a', 501)
        });

        result.ShouldHaveValidationErrorFor(x => x.CustomerQuery);
    }

    [Fact]
    public void Validate_CustomerQueryWithNoAlphabeticContent_ShouldHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto
        {
            AccountId = "ACC-123",
            CustomerQuery = "12345 !!! ###"
        });

        result.ShouldHaveValidationErrorFor(x => x.CustomerQuery);
    }

    [Fact]
    public void Validate_ValidCustomerQuery_ShouldNotHaveError()
    {
        var result = _validator.TestValidate(new QuotationRequestDto
        {
            AccountId = "ACC-123",
            CustomerQuery = "Can you confirm the handling instructions?"
        });

        result.ShouldNotHaveValidationErrorFor(x => x.CustomerQuery);
    }
}
