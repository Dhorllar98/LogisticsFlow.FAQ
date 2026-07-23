using LogisticsFlow.Domain.Entities;

namespace LogisticsFlow.Domain.Interfaces;

public interface IRateAgreementRepository
{
    Task<RateAgreement?> GetCurrentForClientAsync(Guid clientId, CancellationToken ct = default);

    Task<IReadOnlyList<RateAgreement>> GetAllCurrentForClientAsync(Guid clientId, CancellationToken ct = default);

    Task<RateAgreement?> GetByIdForClientAsync(Guid clientId, Guid agreementId, CancellationToken ct = default);
}