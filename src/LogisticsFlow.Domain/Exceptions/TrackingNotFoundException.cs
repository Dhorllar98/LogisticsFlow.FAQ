namespace LogisticsFlow.Domain.Exceptions;

public class TrackingNotFoundException : BusinessRuleException
{
    public string TrackingNumber { get; }

    public TrackingNotFoundException(string trackingNumber)
        : base("No shipment found for tracking number.")
    {
        TrackingNumber = trackingNumber;
    }
}