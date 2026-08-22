namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

public interface OrderSubmissionAccepted
{
    Guid OrderId { get; }
}
