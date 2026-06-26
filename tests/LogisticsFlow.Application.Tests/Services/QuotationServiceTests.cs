using FluentValidation;
using FluentValidation.Results;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Services;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LogisticsFlow.Application.Tests.Services;

public class QuotationServiceTests
{
    private readonly Mock<IClientRepository> _clientRepo = new();
    private readonly Mock<IRateAgreementRepository> _rateRepo = new();
    private readonly Mock<IRedactionService> _redaction = new();
    private readonly Mock<IClaudeApiClient> _claudeClient = new();
    private readonly Mock<IValidator<QuotationResponseDto>> _responseValidator = new();
    private readonly QuotationService _sut;

    public QuotationServiceTests()
    {
        // Default: response always passes validation unless a test
        // overrides this — keeps the happy-path tests focused on what
        // they're actually asserting.
        _responseValidator
            .Setup(v => v.ValidateAsync(It.IsAny<QuotationResponseDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _sut = new QuotationService(
            _clientRepo.Object, _rateRepo.Object, _redaction.Object,
            _claudeClient.Object, _responseValidator.Object);
    }

    private static Client MakeClient(string accountId = "ACC-1", string companyName = "Acme Freight Ltd") =>
        new(Guid.NewGuid(), accountId, companyName);

    private static RateAgreement MakeRateAgreement(Guid clientId) =>
        new(
            id: Guid.NewGuid(),
            clientId: clientId,
            originAddress: "123 Dock Rd, Lagos",
            destinationAddress: "45 Port Ave, Apapa",
            negotiatedRate: 1500m,
            effectiveFrom: DateTime.UtcNow.AddDays(-1),
            specialHandlingInstructions: "Fragile — keep upright");

    [Fact]
    public async Task GetQuotationAsync_ClientNotFound_ThrowsRateAgreementNotFoundException()
    {
        _clientRepo.Setup(r => r.GetByAccountIdAsync("missing", default))
            .ReturnsAsync((Client?)null);

        var request = new QuotationRequestDto { AccountId = "missing" };

        await Assert.ThrowsAsync<RateAgreementNotFoundException>(
            () => _sut.GetQuotationAsync(request));
    }

    [Fact]
    public async Task GetQuotationAsync_NoCurrentRateAgreement_ThrowsRateAgreementNotFoundException()
    {
        var client = MakeClient();
        _clientRepo.Setup(r => r.GetByAccountIdAsync(client.AccountId, default)).ReturnsAsync(client);
        _rateRepo.Setup(r => r.GetCurrentForClientAsync(client.Id, default)).ReturnsAsync((RateAgreement?)null);

        var request = new QuotationRequestDto { AccountId = client.AccountId };

        await Assert.ThrowsAsync<RateAgreementNotFoundException>(
            () => _sut.GetQuotationAsync(request));
    }

    /// <summary>
    /// MANDATORY per Phase 2 test scope: asserts Tier 2 fields (company
    /// name, real address, real rate) never appear unredacted in the
    /// payload sent to the Claude client — only the redacted text should
    /// reach SendMessageAsync.
    /// </summary>
    [Fact]
    public async Task GetQuotationAsync_SendsOnlyRedactedPayloadToClaudeClient_NeverRawTier2Fields()
    {
        var client = MakeClient(companyName: "Acme Freight Ltd");
        var rateAgreement = MakeRateAgreement(client.Id);

        _clientRepo.Setup(r => r.GetByAccountIdAsync(client.AccountId, default)).ReturnsAsync(client);
        _rateRepo.Setup(r => r.GetCurrentForClientAsync(client.Id, default)).ReturnsAsync(rateAgreement);

        const string redactedPayload = "Company: [REDACTED_0]\nOrigin: [REDACTED_1]\nDestination: [REDACTED_2]\nRate: [REDACTED_3]";
        var map = new RedactionMap(new Dictionary<string, string>
        {
            ["[REDACTED_0]"] = client.CompanyName,
            ["[REDACTED_1]"] = rateAgreement.OriginAddress,
            ["[REDACTED_2]"] = rateAgreement.DestinationAddress,
            ["[REDACTED_3]"] = rateAgreement.NegotiatedRate.ToString("C")
        });

        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default))
            .ReturnsAsync((redactedPayload, map));

        string? capturedSystemPrompt = null;
        IReadOnlyList<ChatMessage>? capturedHistory = null;

        _claudeClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .Callback<string, IReadOnlyList<ChatMessage>, CancellationToken>((prompt, history, _) =>
            {
                capturedSystemPrompt = prompt;
                capturedHistory = history;
            })
            .ReturnsAsync("Your quote: Company [REDACTED_0], from [REDACTED_1] to [REDACTED_2] at [REDACTED_3].");

        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default))
            .ReturnsAsync((string text, RedactionMap m, CancellationToken _) => text);

        var request = new QuotationRequestDto { AccountId = client.AccountId };
        await _sut.GetQuotationAsync(request);

        Assert.NotNull(capturedHistory);
        Assert.All(capturedHistory!, m => Assert.Equal(ChatRole.User, m.Role));

        var sentContent = string.Join(" ", capturedHistory!.Select(m => m.Content));

        Assert.DoesNotContain(client.CompanyName, sentContent);
        Assert.DoesNotContain(rateAgreement.OriginAddress, sentContent);
        Assert.DoesNotContain(rateAgreement.DestinationAddress, sentContent);
        Assert.DoesNotContain(rateAgreement.NegotiatedRate.ToString("C"), sentContent);
        Assert.Contains("[REDACTED_0]", sentContent);
    }

    /// <summary>
    /// MANDATORY per Phase 2 test scope: a simulated restore failure must
    /// throw RedactionFailureException, never silently return a partial
    /// or fabricated response.
    /// </summary>
    [Fact]
    public async Task GetQuotationAsync_RestoreFailure_ThrowsRedactionFailureException()
    {
        var client = MakeClient();
        var rateAgreement = MakeRateAgreement(client.Id);

        _clientRepo.Setup(r => r.GetByAccountIdAsync(client.AccountId, default)).ReturnsAsync(client);
        _rateRepo.Setup(r => r.GetCurrentForClientAsync(client.Id, default)).ReturnsAsync(rateAgreement);

        var map = RedactionMap.Empty;
        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default))
            .ReturnsAsync(("redacted text", map));

        _claudeClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("response with [REDACTED_99] that has no matching map entry");

        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default))
            .ThrowsAsync(RedactionFailureException.RestoreMismatch("[REDACTED_99]"));

        var request = new QuotationRequestDto { AccountId = client.AccountId };

        await Assert.ThrowsAsync<RedactionFailureException>(
            () => _sut.GetQuotationAsync(request));
    }

    [Fact]
    public async Task GetQuotationAsync_ResponseFailsValidation_ThrowsBusinessRuleException()
    {
        var client = MakeClient();
        var rateAgreement = MakeRateAgreement(client.Id);

        _clientRepo.Setup(r => r.GetByAccountIdAsync(client.AccountId, default)).ReturnsAsync(client);
        _rateRepo.Setup(r => r.GetCurrentForClientAsync(client.Id, default)).ReturnsAsync(rateAgreement);

        var map = RedactionMap.Empty;
        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default)).ReturnsAsync(("text", map));
        _claudeClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("composed message");
        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default)).ReturnsAsync("composed message");

        _responseValidator
            .Setup(v => v.ValidateAsync(It.IsAny<QuotationResponseDto>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("ComposedMessage", "boom") }));

        var request = new QuotationRequestDto { AccountId = client.AccountId };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetQuotationAsync(request));
    }
}
