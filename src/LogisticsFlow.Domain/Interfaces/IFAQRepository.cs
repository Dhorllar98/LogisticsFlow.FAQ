using LogisticsFlow.Domain.Entities;
using LogisticsFlow.Domain.Enums;

namespace LogisticsFlow.Domain.Interfaces;

/// <summary>
/// Contract for reading the FAQ knowledge base. No implementation
/// details here — Infrastructure decides whether this is backed by a
/// JSON file, a database, or anything else.
/// </summary>
public interface IFAQRepository
{
    Task<IReadOnlyList<FAQEntry>> GetAllAsync();
    Task<IReadOnlyList<FAQEntry>> GetByCategoryAsync(LogisticCategory category);
}