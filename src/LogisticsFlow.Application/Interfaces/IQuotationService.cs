using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Interfaces;

public interface IQuotationService
{
    Task<QuotationResponseDto> GetQuotationAsync(
        QuotationRequestDto request,
        CancellationToken cancellationToken = default);
}
