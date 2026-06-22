using FluentValidation;
using FluentValidation.Results;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Services;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using Moq;
using Xunit;

namespace LogisticsFlow.Application.Tests.Services;

public class FAQServiceTests
{
    private readonly Mock<IFAQRepository> _repoMock = new();
    private readonly Mock<IClaudeApiClient> _claudeMock = new();
    private readonly Mock<IFAQCacheService> _cacheMock = new();
    private readonly Mock<IValidator<FAQResponseDto>> _responseValidatorMock = new();

    private FAQService BuildService()
    {
        return new FAQService(_repoMock.Object, _claudeMock.Object, _cacheMock.Object, _responseValidatorMock.Object);
    }

    private void SetupHappyPathDependencies(string claudeJsonResponse)
    {
        _repoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<FAQEntry> { new() { Id = "L-001", Category = LogisticCategory.Land, Question = "Q", Answer = "A" } });

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _claudeMock.Setup(c => c.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(claudeJsonResponse);

        _responseValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<FAQResponseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    [Fact]
    public async Task AskAsync_WellFormedHighConfidenceResponse_ReturnsNonEscalated()
    {
        var json = """{"answer":"FTL is full truckload.","category":"Land","confidenceScore":0.9,"groundingSources":["L-001"]}""";
        SetupHappyPathDependencies(json);

        var result = await BuildService().AskAsync(new FAQRequestDto { Query = "What is FTL?" });

        Assert.False(result.EscalationBoolean);
        Assert.Equal(LogisticCategory.Land, result.Category);
        Assert.Single(result.GroundingSources);
    }

    [Fact]
    public async Task AskAsync_EmptyGroundingSources_ForcesEscalationTrueRegardlessOfConfidence()
    {
        var json = """{"answer":"I'm not sure.","category":"General","confidenceScore":0.95,"groundingSources":[]}""";
        SetupHappyPathDependencies(json);

        var result = await BuildService().AskAsync(new FAQRequestDto { Query = "What is the meaning of life?" });

        Assert.True(result.EscalationBoolean);
    }

    [Fact]
    public async Task AskAsync_ConfidenceBelowThreshold_ForcesEscalationTrue()
    {
        var json = """{"answer":"Possibly this.","category":"General","confidenceScore":0.5,"groundingSources":["L-001"]}""";
        SetupHappyPathDependencies(json);

        var result = await BuildService().AskAsync(new FAQRequestDto { Query = "Some ambiguous question" });

        Assert.True(result.EscalationBoolean);
    }

    [Fact]
    public async Task AskAsync_MalformedJsonFromClaude_ThrowsBusinessRuleException()
    {
        SetupHappyPathDependencies("this is not valid json");

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => BuildService().AskAsync(new FAQRequestDto { Query = "What is FTL?" }));
    }

    [Fact]
    public async Task AskAsync_UnrecognizedCategory_ThrowsBusinessRuleException()
    {
        var json = """{"answer":"Some answer.","category":"Space","confidenceScore":0.9,"groundingSources":["L-001"]}""";
        SetupHappyPathDependencies(json);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => BuildService().AskAsync(new FAQRequestDto { Query = "What is FTL?" }));
    }

    [Fact]
    public async Task AskAsync_StandaloneQuery_IsCacheChecked()
    {
        var json = """{"answer":"FTL is full truckload.","category":"Land","confidenceScore":0.9,"groundingSources":["L-001"]}""";
        SetupHappyPathDependencies(json);

        await BuildService().AskAsync(new FAQRequestDto { Query = "What is FTL?" });

        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AskAsync_QueryWithHistory_IsNeverCached()
    {
        var json = """{"answer":"Follow-up answer.","category":"Land","confidenceScore":0.9,"groundingSources":["L-001"]}""";
        SetupHappyPathDependencies(json);

        var history = new List<ChatMessage> { new() { Role = ChatRole.User, Content = "earlier question" } };
        await BuildService().AskAsync(new FAQRequestDto { Query = "follow-up question", History = history });

        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AskAsync_ResponseValidatorFails_ThrowsBusinessRuleException()
    {
        var json = """{"answer":"FTL is full truckload.","category":"Land","confidenceScore":0.9,"groundingSources":["L-001"]}""";
        _repoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<FAQEntry> { new() { Id = "L-001", Category = LogisticCategory.Land, Question = "Q", Answer = "A" } });
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _claudeMock.Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
        _responseValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<FAQResponseDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Answer", "too short") }));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => BuildService().AskAsync(new FAQRequestDto { Query = "What is FTL?" }));
    }
}
