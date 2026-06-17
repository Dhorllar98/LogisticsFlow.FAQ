using LogisticsFlow.Domain.Enums;

namespace LogisticsFlow.Domain.Entities;

/// <summary>
/// A single grounded knowledge base record. This is the unit of truth
/// the AI is restricted to when answering — see RAG-lite approach in
/// docs/architecture.md.
/// </summary>
public class FAQEntry
{
    public string Id { get; set; } = string.Empty;
    public LogisticCategory Category { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}