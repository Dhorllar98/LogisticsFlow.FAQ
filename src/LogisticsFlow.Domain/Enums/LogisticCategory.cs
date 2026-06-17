namespace LogisticsFlow.Domain.Enums;

/// <summary>
/// Classifies a piece of logistics knowledge by transport mode.
/// Matches the "Category" values used in faq_knowledgebase.json.
/// </summary>
public enum LogisticCategory
{
    Land,
    Sea,
    Air,
    General
}