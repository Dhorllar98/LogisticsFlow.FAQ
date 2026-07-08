using System.Text;
using FluentValidation;
using LogisticsFlow.Application.DTOs;
using LogisticsFlow.Application.Interfaces;
using LogisticsFlow.Application.Prompts;
using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Exceptions;
using LogisticsFlow.Domain.Interfaces;

namespace LogisticsFlow.Application.Services;

public class TrackingService : ITrackingService
{
    private readonly ITrackingRepository _trackingRepository;
    private readonly IRedactionService _redactionService;
    private readonly ILlmClient _llmClient;
    private readonly IValidator<TrackingResponseDto> _responseValidator;

    public TrackingService(
        ITrackingRepository trackingRepository,
        IRedactionService redactionService,
        ILlmClient llmClient,
        IValidator<TrackingResponseDto> responseValidator)
    {
        _trackingRepository = trackingRepository;
        _redactionService = redactionService;
        _llmClient = llmClient;
        _responseValidator = responseValidator;
    }

    public async Task<TrackingResponseDto> GetStatusAsync(
        TrackingRequestDto request,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _trackingRepository.GetByTrackingNumberForAccountAsync(
            request.TrackingNumber, accountId, cancellationToken);

        if (shipment is null)
        {
            throw new TrackingNotFoundException(request.TrackingNumber);
        }

        var tier2Block = BuildTier2Block(shipment, accountId);
        var tier1Block = BuildTier1Block(shipment);

        var (redactedTier2Block, redactionMap) =
            await _redactionService.RedactAsync(tier2Block, cancellationToken);

        var promptContent = $"{redactedTier2Block}\n{tier1Block}";

        var conversationHistory = new List<ChatMessage>
        {
            new() { Role = ChatRole.User, Content = promptContent }
        };

        var rawResponse = await _llmClient.SendMessageAsync(
            SystemPrompts.TrackingStatusSystemPrompt,
            conversationHistory,
            cancellationToken);

        var statusSummary = await _redactionService.RestoreAsync(
            rawResponse, redactionMap, cancellationToken);

        var response = new TrackingResponseDto
        {
            TrackingNumber = shipment.TrackingNumber,
            Carrier = shipment.Carrier,
            Mode = shipment.Mode.ToString(),
            StatusSummary = statusSummary,
            LastUpdatedUtc = shipment.Events.Count > 0
                ? shipment.Events.Max(e => e.TimestampUtc)
                : shipment.CreatedAtUtc
        };

        var validationResult = await _responseValidator.ValidateAsync(response, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BusinessRuleException($"Tracking response failed validation: {errors}");
        }

        return response;
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

        var eventsWithNotes = shipment.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Notes))
            .ToList();

        for (var i = 0; i < eventsWithNotes.Count; i++)
        {
            var collapsedNote = eventsWithNotes[i].Notes!
                .Replace("\r\n", " ").Replace("\n", " ").Trim();
            lines.Add($"Event Note {i}: {collapsedNote}");
        }

        return string.Join("\n", lines);
    }

    private static string BuildTier1Block(Shipment shipment)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Carrier: {shipment.Carrier}");
        sb.AppendLine($"Mode: {shipment.Mode}");
        sb.AppendLine("Tracking Events (chronological):");

        foreach (var evt in shipment.Events.OrderBy(e => e.TimestampUtc))
        {
            sb.AppendLine($"- {evt.TimestampUtc:u} | {evt.MilestoneType} | {evt.Location}");
        }

        return sb.ToString();
    }
}