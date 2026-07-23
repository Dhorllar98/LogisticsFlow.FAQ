using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Interfaces;

public interface IQuotationService
{
    Task<QuotationResponseDto> GetQuotationAsync(
        QuotationRequestDto request,
        string accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists every currently effective rate agreement for the
    /// authenticated account, for use in a multi-agreement selector
    /// before calling GetQuotationAsync.
    /// </summary>
    Task<IReadOnlyList<RateAgreementSummaryDto>> GetAgreementsAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}