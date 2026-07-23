namespace LogisticsFlow.Application.DTOs;

public class QuotationRequestDto
{
    public Guid? AgreementId { get; set; }

    public string? CustomerQuery { get; set; }
}