using System.Text;
using FluentValidation;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using LogisticsFlow.Application.Prompts;
using LogisticsFlow.Domain.Constants;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;
using LogisticsFlow.Domain.ValueObjects;

namespace LogisticsFlow.Application.Services;

public class RiskAssessmentService : IRiskAssessmentService
{
    // Elevated when elapsed time exceeds the lane average by this factor.
    // A single named constant, not a magic number - easy to find, tune,
    // and unit test in isolation from prompt/AI behavior.
    private const double ElevatedRiskMultiplier = 1.5;

    private readonly ITrackingRepository _trackingRepository;
    private readonly ILaneHistoryRepository _laneHistoryRepository;
    private readonly IRedactionService _redactionService;
    private readonly ILlmClient _llmClient;
    private readonly IValidator<RiskAssessmentResponseDto> _responseValidator;

    public RiskAssessmentService(
        ITrackingRepository trackingRepository,
        ILaneHistoryRepository laneHistoryRepository,
        IRedactionService redactionService,
        ILlmClient llmClient,
        IValidator<RiskAssessmentResponseDto> responseValidator)
    {
        _trackingRepository = trackingRepository;
        _laneHistoryRepository = laneHistoryRepository;
        _redactionService = redactionService;
        _llmClient = llmClient;
        _responseValidator = responseValidator;
    }

    public async Task<RiskAssessmentResponseDto> AssessAsync(
        RiskAssessmentRequestDto request,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _trackingRepository.GetByTrackingNumberForAccountAsync(
            request.TrackingNumber, accountId, cancellationToken);

        if (shipment is null)
        {
            throw new TrackingNotFoundException(request.TrackingNumber);
        }

        var isDelivered = shipment.Events.Any(e => e.MilestoneType == MilestoneTypes.Delivered);
        var elapsedDays = ComputeElapsedDays(shipment, isDelivered);

        var laneStats = await _laneHistoryRepository.GetLaneStatsAsync(
            shipment.Carrier, shipment.Mode, shipment.OriginRegion, shipment.DestinationRegion,
            cancellationToken);

        var riskLevel = DetermineRiskLevel(isDelivered, elapsedDays, laneStats);

        var tier2Block = BuildTier2Block(shipment, accountId);
        var tier1Block = BuildTier1Block(shipment, isDelivered, elapsedDays, laneStats, riskLevel);

        var (redactedTier2Block, redactionMap) =
            await _redactionService.RedactAsync(tier2Block, cancellationToken);

        var promptContent = $"{redactedTier2Block}\n{tier1Block}";

        var conversationHistory = new List<ChatMessage>
        {
            new() { Role = ChatRole.User, Content = promptContent }
        };

        var rawResponse = await _llmClient.SendMessageAsync(
            SystemPrompts.RiskAssessmentSuggestedActionPrompt,
            conversationHistory,
            cancellationToken);

        var suggestedAction = await _redactionService.RestoreAsync(
            rawResponse, redactionMap, cancellationToken);

        var response = new RiskAssessmentResponseDto
        {
            TrackingNumber = shipment.TrackingNumber,
            Carrier = shipment.Carrier,
            Mode = shipment.Mode.ToString(),
            ElapsedDays = Math.Round(elapsedDays, 1),
            LaneAverageDays = laneStats is null ? null : Math.Round(laneStats.AverageTransitDays, 1),
            SampleSize = laneStats?.SampleSize ?? 0,
            RiskLevel = riskLevel.ToString(),
            SuggestedAction = suggestedAction
        };

        var validationResult = await _responseValidator.ValidateAsync(response, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BusinessRuleException($"Risk assessment response failed validation: {errors}");
        }

        return response;
    }

    /// <summary>
    /// Delivered shipments get a fixed, correct duration. In-transit
    /// shipments get elapsed time "so far," which keeps growing until
    /// delivery.
    /// </summary>
    private static double ComputeElapsedDays(Shipment shipment, bool isDelivered)
    {
        if (isDelivered)
        {
            var deliveredAt = shipment.Events
                .Where(e => e.MilestoneType == MilestoneTypes.Delivered)
                .Min(e => e.TimestampUtc);
            return (deliveredAt - shipment.CreatedAtUtc).TotalDays;
        }

        return (DateTime.UtcNow - shipment.CreatedAtUtc).TotalDays;
    }

    /// <summary>
    /// Deterministic business rule, deliberately kept as plain C# rather
    /// than AI-decided - see CLAUDE.md Phase 3.5 section. Directly
    /// unit-testable without mocking any AI response. A delivered
    /// shipment is always Normal - risk assessment only applies to
    /// shipments still in transit; there is nothing actionable left to
    /// flag once a shipment has arrived.
    /// </summary>
    private static RiskLevel DetermineRiskLevel(
        bool isDelivered, double elapsedDays, LaneHistoryResult? laneStats)
    {
        if (isDelivered)
        {
            return RiskLevel.Normal;
        }

        if (laneStats is null)
        {
            return RiskLevel.Unknown;
        }

        return elapsedDays > laneStats.AverageTransitDays * ElevatedRiskMultiplier
            ? RiskLevel.Elevated
            : RiskLevel.Normal;
    }

    private static string BuildTier2Block(Shipment shipment, string accountId)
    {
        var lines = new List<string>
        {
            $"Account ID: {accountId}",
            $"Tracking Number: {shipment.TrackingNumber}",
            $"Origin Address: {shipment.OriginAddress}",
            $"Destination Address: {shipment.DestinationAddress}",
            $"Consignee Name: {shipment.ConsigneeName}",
            $"Consignee Address: {shipment.ConsigneeAddress}"
        };

        return string.Join("\n", lines);
    }

    private static string BuildTier1Block(
        Shipment shipment, bool isDelivered, double elapsedDays,
        LaneHistoryResult? laneStats, RiskLevel riskLevel)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Carrier: {shipment.Carrier}");
        sb.AppendLine($"Mode: {shipment.Mode}");
        sb.AppendLine(isDelivered
            ? $"Total Transit Days (delivered): {Math.Round(elapsedDays, 1)}"
            : $"Elapsed Days So Far (still in transit): {Math.Round(elapsedDays, 1)}");

        if (laneStats is not null)
        {
            sb.AppendLine($"Lane Average Transit Days: {Math.Round(laneStats.AverageTransitDays, 1)}");
            sb.AppendLine($"Lane Sample Size: {laneStats.SampleSize}");
        }
        else
        {
            sb.AppendLine("Lane Average Transit Days: insufficient historical data for this lane");
        }

        sb.AppendLine($"Risk Level (already determined - do not re-evaluate): {riskLevel}");

        return sb.ToString();
    }
}