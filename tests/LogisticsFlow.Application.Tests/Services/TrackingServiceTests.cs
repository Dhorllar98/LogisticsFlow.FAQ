using FluentValidation;
using FluentValidation.Results;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Services;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LogisticsFlow.Application.Tests.Services;

public class TrackingServiceTests
{
    private readonly Mock<ITrackingRepository> _repo = new();
    private readonly Mock<IRedactionService> _redaction = new();
    private readonly Mock<ILlmClient> _llmClient = new();
    private readonly Mock<IValidator<TrackingResponseDto>> _responseValidator = new();
    private readonly TrackingService _sut;

    public TrackingServiceTests()
    {
        _responseValidator
            .Setup(v => v.ValidateAsync(It.IsAny<TrackingResponseDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _sut = new TrackingService(_repo.Object, _redaction.Object, _llmClient.Object, _responseValidator.Object);
    }

    private static Shipment MakeShipment() => new()
    {
        Id = Guid.NewGuid(),
        TrackingNumber = "TRK-TEST-001",
        ClientId = Guid.NewGuid(),
        Carrier = "Maersk Line",
        Mode = ShipmentMode.Sea,
        OriginAddress = "123 Dock Rd, Lagos",
        DestinationAddress = "45 Port Ave, Apapa",
        ConsigneeName = "John Doe",
        ConsigneeAddress = "45 Port Ave, Apapa, Lagos",
        CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
        Events = new List<TrackingEvent>
        {
            new() { Id = Guid.NewGuid(), MilestoneType = "DepartedOrigin", Location = "Lagos Port", TimestampUtc = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), MilestoneType = "CustomsHold", Location = "Apapa Customs", TimestampUtc = DateTime.UtcNow.AddDays(-1), Notes = "Held pending updated invoice" }
        }
    };

    [Fact]
    public async Task GetStatusAsync_UnknownTrackingNumber_ThrowsTrackingNotFoundException()
    {
        _repo.Setup(r => r.GetByTrackingNumberForAccountAsync("TRK-MISSING", "ACC-1", default))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<TrackingNotFoundException>(
            () => _sut.GetStatusAsync(new TrackingRequestDto { TrackingNumber = "TRK-MISSING" }, "ACC-1"));
    }

    [Fact]
    public async Task GetStatusAsync_TrackingNumberBelongsToDifferentAccount_ThrowsTrackingNotFoundException()
    {
        // The repository's join enforces scoping - a wrong account and a
        // nonexistent tracking number are indistinguishable at this layer,
        // exactly as designed.
        _repo.Setup(r => r.GetByTrackingNumberForAccountAsync("TRK-TEST-001", "ACC-WRONG", default))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<TrackingNotFoundException>(
            () => _sut.GetStatusAsync(new TrackingRequestDto { TrackingNumber = "TRK-TEST-001" }, "ACC-WRONG"));
    }

    [Fact]
    public async Task GetStatusAsync_Tier1FieldsReachLlmInClear_Tier2FieldsNeverDo()
    {
        var shipment = MakeShipment();
        _repo.Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, "ACC-1", default))
            .ReturnsAsync(shipment);

        const string redactedPayload = "Account ID: [REDACTED_0]\nTracking Number: [REDACTED_1]\nOrigin Address: [REDACTED_2]";
        var map = new RedactionMap(new Dictionary<string, string>
        {
            ["[REDACTED_0]"] = "ACC-1",
            ["[REDACTED_1]"] = shipment.TrackingNumber,
            ["[REDACTED_2]"] = shipment.OriginAddress
        });

        string? capturedTier2Input = null;
        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default))
            .Callback<string, CancellationToken>((text, _) => capturedTier2Input = text)
            .ReturnsAsync((redactedPayload, map));

        string? capturedPromptContent = null;
        _llmClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .Callback<string, IReadOnlyList<ChatMessage>, CancellationToken>((_, history, _) =>
                capturedPromptContent = string.Join(" ", history.Select(m => m.Content)))
            .ReturnsAsync("Shipment departed and is held at [REDACTED_2] customs.");

        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default))
            .ReturnsAsync((string text, RedactionMap m, CancellationToken _) => text);

        await _sut.GetStatusAsync(new TrackingRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Contains(shipment.OriginAddress, capturedTier2Input);
        Assert.Contains(shipment.Carrier, capturedPromptContent);
        Assert.Contains(shipment.Mode.ToString(), capturedPromptContent);
        Assert.DoesNotContain(shipment.OriginAddress, capturedPromptContent);
        Assert.Contains("[REDACTED_2]", capturedPromptContent);
    }

    [Fact]
    public async Task GetStatusAsync_RestoreFailure_ThrowsRedactionFailureException()
    {
        var shipment = MakeShipment();
        _repo.Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, "ACC-1", default))
            .ReturnsAsync(shipment);

        var map = RedactionMap.Empty;
        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default)).ReturnsAsync(("redacted", map));
        _llmClient.Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("response with [REDACTED_99] and no matching map entry");
        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default))
            .ThrowsAsync(RedactionFailureException.RestoreMismatch("[REDACTED_99]"));

        await Assert.ThrowsAsync<RedactionFailureException>(
            () => _sut.GetStatusAsync(new TrackingRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1"));
    }

    [Fact]
    public async Task GetStatusAsync_ResponseFailsValidation_ThrowsBusinessRuleException()
    {
        var shipment = MakeShipment();
        _repo.Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, "ACC-1", default))
            .ReturnsAsync(shipment);

        var map = RedactionMap.Empty;
        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default)).ReturnsAsync(("redacted", map));
        _llmClient.Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("");
        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default)).ReturnsAsync("");

        _responseValidator
            .Setup(v => v.ValidateAsync(It.IsAny<TrackingResponseDto>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("StatusSummary", "must not be empty") }));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.GetStatusAsync(new TrackingRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1"));
    }

    [Fact]
    public async Task GetStatusAsync_NoEventsYet_LastUpdatedFallsBackToCreatedAtUtc()
    {
        var shipment = MakeShipment();
        shipment.Events = new List<TrackingEvent>();
        _repo.Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, "ACC-1", default))
            .ReturnsAsync(shipment);

        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default)).ReturnsAsync(("redacted", RedactionMap.Empty));
        _llmClient.Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("No events yet.");
        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), RedactionMap.Empty, default)).ReturnsAsync("No events yet.");

        var result = await _sut.GetStatusAsync(new TrackingRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Equal(shipment.CreatedAtUtc, result.LastUpdatedUtc);
    }
}