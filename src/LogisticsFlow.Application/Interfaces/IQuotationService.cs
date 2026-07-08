using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Interfaces;

public interface IQuotationService
{
    Task<QuotationResponseDto> GetQuotationAsync(
        QuotationRequestDto request,
        string accountId,
        CancellationToken cancellationToken = default);
}