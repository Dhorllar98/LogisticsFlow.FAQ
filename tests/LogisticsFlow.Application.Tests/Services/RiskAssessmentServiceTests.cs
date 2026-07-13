using FluentValidation;
using FluentValidation.Results;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Services;
using LogisticsFlow.Domain.Constants;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LogisticsFlow.Application.Tests.Services;

public class RiskAssessmentServiceTests
{
    private readonly Mock<ITrackingRepository> _trackingRepo = new();
    private readonly Mock<ILaneHistoryRepository> _laneHistoryRepo = new();
    private readonly Mock<IRedactionService> _redaction = new();
    private readonly Mock<ILlmClient> _llmClient = new();
    private readonly Mock<IValidator<RiskAssessmentResponseDto>> _responseValidator = new();
    private readonly RiskAssessmentService _sut;

    public RiskAssessmentServiceTests()
    {
        _responseValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RiskAssessmentResponseDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _sut = new RiskAssessmentService(
            _trackingRepo.Object, _laneHistoryRepo.Object, _redaction.Object,
            _llmClient.Object, _responseValidator.Object);
    }

    private static Shipment MakeShipment(
        DateTime createdAtUtc,
        bool isDelivered,
        DateTime? deliveredAtUtc = null) => new()
    {
        Id = Guid.NewGuid(),
        TrackingNumber = "TRK-RISK-001",
        ClientId = Guid.NewGuid(),
        Carrier = "Maersk Line",
        Mode = ShipmentMode.Sea,
        OriginAddress = "123 Dock Rd, Lagos",
        DestinationAddress = "45 Port Ave, Apapa",
        OriginRegion = "Lagos",
        DestinationRegion = "Apapa",
        ConsigneeName = "John Doe",
        ConsigneeAddress = "45 Port Ave, Apapa, Lagos",
        CreatedAtUtc = createdAtUtc,
        Events = isDelivered
            ? new List<TrackingEvent>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    MilestoneType = MilestoneTypes.Delivered,
                    Location = "Apapa Terminal",
                    TimestampUtc = deliveredAtUtc ?? DateTime.UtcNow
                }
            }
            : new List<TrackingEvent>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    MilestoneType = "DepartedOrigin",
                    Location = "Lagos Port",
                    TimestampUtc = createdAtUtc.AddHours(2)
                }
            }
    };

    private void SetupHappyPath(Shipment shipment, string accountId, LaneHistoryResult? laneStats)
    {
        _trackingRepo
            .Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, accountId, default))
            .ReturnsAsync(shipment);

        _laneHistoryRepo
            .Setup(r => r.GetLaneStatsAsync(
                shipment.Carrier, shipment.Mode, shipment.OriginRegion, shipment.DestinationRegion, default))
            .ReturnsAsync(laneStats);

        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default))
            .ReturnsAsync(("redacted", RedactionMap.Empty));
        _llmClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("Suggested action text.");
        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), RedactionMap.Empty, default))
            .ReturnsAsync("Suggested action text.");
    }

    [Fact]
    public async Task AssessAsync_UnknownTrackingNumber_ThrowsTrackingNotFoundException()
    {
        _trackingRepo
            .Setup(r => r.GetByTrackingNumberForAccountAsync("TRK-MISSING", "ACC-1", default))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<TrackingNotFoundException>(
            () => _sut.AssessAsync(new RiskAssessmentRequestDto { TrackingNumber = "TRK-MISSING" }, "ACC-1"));
    }

    [Fact]
    public async Task AssessAsync_TrackingNumberBelongsToDifferentAccount_ThrowsTrackingNotFoundException()
    {
        _trackingRepo
            .Setup(r => r.GetByTrackingNumberForAccountAsync("TRK-RISK-001", "ACC-WRONG", default))
            .ReturnsAsync((Shipment?)null);

        await Assert.ThrowsAsync<TrackingNotFoundException>(
            () => _sut.AssessAsync(new RiskAssessmentRequestDto { TrackingNumber = "TRK-RISK-001" }, "ACC-WRONG"));
    }

    [Fact]
    public async Task AssessAsync_DeliveredShipment_AlwaysReturnsNormalRegardlessOfDuration()
    {
        // Delivered after 20 days - would be Elevated if still evaluated
        // against a short lane average, but a delivered shipment has
        // nothing actionable left to flag.
        var createdAt = DateTime.UtcNow.AddDays(-20);
        var deliveredAt = DateTime.UtcNow;
        var shipment = MakeShipment(createdAt, isDelivered: true, deliveredAt);

        SetupHappyPath(shipment, "ACC-1", new LaneHistoryResult(AverageTransitDays: 4, SampleSize: 5));

        var result = await _sut.AssessAsync(
            new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Equal(RiskLevel.Normal.ToString(), result.RiskLevel);
        Assert.Equal(20, result.ElapsedDays, precision: 0);
    }

    [Fact]
    public async Task AssessAsync_InTransitNoLaneHistory_ReturnsUnknown()
    {
        var shipment = MakeShipment(DateTime.UtcNow.AddDays(-3), isDelivered: false);

        SetupHappyPath(shipment, "ACC-1", laneStats: null);

        var result = await _sut.AssessAsync(
            new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Equal(RiskLevel.Unknown.ToString(), result.RiskLevel);
        Assert.Null(result.LaneAverageDays);
        Assert.Equal(0, result.SampleSize);
    }

    [Fact]
    public async Task AssessAsync_InTransitWithinThreshold_ReturnsNormal()
    {
        // Lane average 4 days, elapsed 5 days -> 5 <= 4 * 1.5 (6) -> Normal
        var shipment = MakeShipment(DateTime.UtcNow.AddDays(-5), isDelivered: false);

        SetupHappyPath(shipment, "ACC-1", new LaneHistoryResult(AverageTransitDays: 4, SampleSize: 5));

        var result = await _sut.AssessAsync(
            new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Equal(RiskLevel.Normal.ToString(), result.RiskLevel);
        Assert.Equal(4, result.LaneAverageDays);
        Assert.Equal(5, result.SampleSize);
    }

    [Fact]
    public async Task AssessAsync_InTransitExceedsThreshold_ReturnsElevated()
    {
        // Lane average 4 days, elapsed 7 days -> 7 > 4 * 1.5 (6) -> Elevated
        var shipment = MakeShipment(DateTime.UtcNow.AddDays(-7), isDelivered: false);

        SetupHappyPath(shipment, "ACC-1", new LaneHistoryResult(AverageTransitDays: 4, SampleSize: 5));

        var result = await _sut.AssessAsync(
            new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Equal(RiskLevel.Elevated.ToString(), result.RiskLevel);
    }

    [Fact]
    public async Task AssessAsync_Tier2FieldsNeverReachLlmInClear()
    {
        var shipment = MakeShipment(DateTime.UtcNow.AddDays(-3), isDelivered: false);

        _trackingRepo
            .Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, "ACC-1", default))
            .ReturnsAsync(shipment);
        _laneHistoryRepo
            .Setup(r => r.GetLaneStatsAsync(
                shipment.Carrier, shipment.Mode, shipment.OriginRegion, shipment.DestinationRegion, default))
            .ReturnsAsync(new LaneHistoryResult(AverageTransitDays: 4, SampleSize: 5));

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
            .ReturnsAsync("Your shipment is progressing normally at [REDACTED_2].");

        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default))
            .ReturnsAsync((string text, RedactionMap m, CancellationToken _) => text);

        await _sut.AssessAsync(new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1");

        Assert.Contains(shipment.OriginAddress, capturedTier2Input);
        Assert.Contains(shipment.Carrier, capturedPromptContent);
        Assert.DoesNotContain(shipment.OriginAddress, capturedPromptContent);
        Assert.Contains("[REDACTED_2]", capturedPromptContent);
    }

    [Fact]
    public async Task AssessAsync_RestoreFailure_ThrowsRedactionFailureException()
    {
        var shipment = MakeShipment(DateTime.UtcNow.AddDays(-3), isDelivered: false);

        _trackingRepo
            .Setup(r => r.GetByTrackingNumberForAccountAsync(shipment.TrackingNumber, "ACC-1", default))
            .ReturnsAsync(shipment);
        _laneHistoryRepo
            .Setup(r => r.GetLaneStatsAsync(
                shipment.Carrier, shipment.Mode, shipment.OriginRegion, shipment.DestinationRegion, default))
            .ReturnsAsync(new LaneHistoryResult(AverageTransitDays: 4, SampleSize: 5));

        var map = RedactionMap.Empty;
        _redaction.Setup(r => r.RedactAsync(It.IsAny<string>(), default)).ReturnsAsync(("redacted", map));
        _llmClient.Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ChatMessage>>(), default))
            .ReturnsAsync("response with [REDACTED_99] and no matching map entry");
        _redaction.Setup(r => r.RestoreAsync(It.IsAny<string>(), map, default))
            .ThrowsAsync(RedactionFailureException.RestoreMismatch("[REDACTED_99]"));

        await Assert.ThrowsAsync<RedactionFailureException>(
            () => _sut.AssessAsync(new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1"));
    }

    [Fact]
    public async Task AssessAsync_ResponseFailsValidation_ThrowsBusinessRuleException()
    {
        var shipment = MakeShipment(DateTime.UtcNow.AddDays(-3), isDelivered: false);

        SetupHappyPath(shipment, "ACC-1", new LaneHistoryResult(AverageTransitDays: 4, SampleSize: 5));

        _responseValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RiskAssessmentResponseDto>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("SuggestedAction", "must not be empty") }));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.AssessAsync(new RiskAssessmentRequestDto { TrackingNumber = shipment.TrackingNumber }, "ACC-1"));
    }
}