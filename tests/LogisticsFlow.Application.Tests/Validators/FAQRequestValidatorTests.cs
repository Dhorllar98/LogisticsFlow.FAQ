using FluentValidation.TestHelper;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Validators;
using LogisticsFlow.Domain.Entities;
using Xunit;

namespace LogisticsFlow.Application.Tests.Validators;

public class FAQRequestValidatorTests
{
    private readonly FAQRequestValidator _validator = new();

    [Fact]
    public void Validate_EmptyQuery_HasError()
    {
        var result = _validator.TestValidate(new FAQRequestDto { Query = "" });
        result.ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Validate_QueryTooShort_HasError()
    {
        var result = _validator.TestValidate(new FAQRequestDto { Query = "hi" });
        result.ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Validate_QueryExceedsMaxLength_HasError()
    {
        var longQuery = new string('a', 501);
        var result = _validator.TestValidate(new FAQRequestDto { Query = longQuery });
        result.ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Validate_QueryAtMaxLength_HasNoError()
    {
        var maxQuery = new string('a', 500);
        var result = _validator.TestValidate(new FAQRequestDto { Query = maxQuery });
        result.ShouldNotHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Validate_QueryWithNoAlphabeticContent_HasError()
    {
        var result = _validator.TestValidate(new FAQRequestDto { Query = "!!!!!!!!" });
        result.ShouldHaveValidationErrorFor(x => x.Query);
    }

    [Fact]
    public void Validate_ValidQuery_HasNoErrors()
    {
        var result = _validator.TestValidate(new FAQRequestDto { Query = "What is FTL shipping?" });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_HistoryExceedsMaxEntries_HasError()
    {
        var history = Enumerable.Range(0, 7)
            .Select(i => new ChatMessage { Role = ChatRole.User, Content = $"message {i}" })
            .ToList();

        var result = _validator.TestValidate(new FAQRequestDto { Query = "valid query", History = history });
        result.ShouldHaveValidationErrorFor(x => x.History);
    }

    [Fact]
    public void Validate_HistoryEntryWithEmptyContent_HasError()
    {
        var history = new List<ChatMessage> { new() { Role = ChatRole.User, Content = "" } };
        var result = _validator.TestValidate(new FAQRequestDto { Query = "valid query", History = history });
        result.ShouldHaveValidationErrorFor("History[0].Content");
    }

    [Fact]
    public void Validate_HistoryEntryExceedsMaxLength_HasError()
    {
        var history = new List<ChatMessage> { new() { Role = ChatRole.User, Content = new string('b', 501) } };
        var result = _validator.TestValidate(new FAQRequestDto { Query = "valid query", History = history });
        result.ShouldHaveValidationErrorFor("History[0].Content");
    }

    [Fact]
    public void Validate_NullHistory_HasNoError()
    {
        var result = _validator.TestValidate(new FAQRequestDto { Query = "valid query", History = null });
        result.ShouldNotHaveValidationErrorFor(x => x.History);
    }
}
