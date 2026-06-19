using LogisticsFlow.Application.DTOs;

namespace LogisticsFlow.Application.Interfaces;

public interface IFAQService
{
    /// <summary>
    /// Processes a validated user query against the grounded knowledge
    /// base and returns a shaped, validated response.
    /// </summary>
    Task<FAQResponseDto> AskAsync(FAQRequestDto request, CancellationToken cancellationToken = default);
}