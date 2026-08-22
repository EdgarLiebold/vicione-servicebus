namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

[ExcludeFromTopology]
public interface OrderEvent :
    CorrelatedBy<Guid>
{
    Guid OrderId { get; }
    DateTime Timestamp { get; }
}
