using FluentValidation.TestHelper;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Validators;
using LogisticsFlow.Domain.Enums;
using Xunit;

namespace LogisticsFlow.Application.Tests.Validators;

public class FAQResponseValidatorTests
{
    private readonly FAQResponseValidator _validator = new();

    private static FAQResponseDto ValidResponse() => new()
    {
        Answer = "FTL means Full Truckload shipping.",
        Category = LogisticCategory.Land,
        ConfidenceScore = 0.9,
        GroundingSources = new List<string> { "L-001" },
        EscalationBoolean = false
    };

    [Fact]
    public void Validate_WellFormedHighConfidenceResponse_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidResponse());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyAnswer_HasError()
    {
        var response = ValidResponse();
        response.Answer = "";
        var result = _validator.TestValidate(response);
        result.ShouldHaveValidationErrorFor(x => x.Answer);
    }

    [Fact]
    public void Validate_ConfidenceScoreOutOfRange_HasError()
    {
        var response = ValidResponse();
        response.ConfidenceScore = 1.5;
        var result = _validator.TestValidate(response);
        result.ShouldHaveValidationErrorFor(x => x.ConfidenceScore);
    }

    [Fact]
    public void Validate_LowConfidenceWithoutEscalation_HasError()
    {
        var response = ValidResponse();
        response.ConfidenceScore = 0.5;
        response.EscalationBoolean = false;
        var result = _validator.TestValidate(response);
        result.ShouldHaveValidationErrorFor(x => x.EscalationBoolean);
    }

    [Fact]
    public void Validate_EmptyGroundingSourcesWithoutEscalation_HasError()
    {
        var response = ValidResponse();
        response.GroundingSources = new List<string>();
        response.EscalationBoolean = false;
        var result = _validator.TestValidate(response);
        result.ShouldHaveValidationErrorFor(x => x.EscalationBoolean);
    }

    [Fact]
    public void Validate_LowConfidenceWithEscalationTrue_HasNoError()
    {
        var response = ValidResponse();
        response.ConfidenceScore = 0.3;
        response.GroundingSources = new List<string>();
        response.EscalationBoolean = true;
        var result = _validator.TestValidate(response);
        result.ShouldNotHaveValidationErrorFor(x => x.EscalationBoolean);
    }
}
