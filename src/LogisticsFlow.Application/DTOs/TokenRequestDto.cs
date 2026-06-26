namespace LogisticsFlow.Application.DTOs;

public class TokenRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}