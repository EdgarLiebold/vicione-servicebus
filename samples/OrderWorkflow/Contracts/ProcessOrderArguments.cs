namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

public interface ProcessOrderArguments
{
    Guid OrderId { get; }
}
