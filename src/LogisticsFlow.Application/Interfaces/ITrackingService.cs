using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Interfaces;

public interface ITrackingService
{
    Task<TrackingResponseDto> GetStatusAsync(
        TrackingRequestDto request,
        string accountId,
        CancellationToken cancellationToken = default);
}