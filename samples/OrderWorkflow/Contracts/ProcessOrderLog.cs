namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

public interface ProcessOrderLog
{
    Guid OrderId { get; }
    Guid ShipmentId { get; }
}
