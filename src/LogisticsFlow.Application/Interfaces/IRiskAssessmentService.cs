using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Interfaces;

public interface IRiskAssessmentService
{
    Task<RiskAssessmentResponseDto> AssessAsync(
        RiskAssessmentRequestDto request,
        string accountId,
        CancellationToken cancellationToken = default);
}