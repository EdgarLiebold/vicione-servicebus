namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

[ExcludeFromTopology]
public interface OrderEvent :
    ICorrelatedBy<Guid>
{
    Guid OrderId { get; }
    DateTime Timestamp { get; }
}
