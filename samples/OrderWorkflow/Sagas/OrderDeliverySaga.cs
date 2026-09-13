using ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Sagas;

public sealed class OrderDeliverySaga :
    ISaga,
    IInitiatedByOrOrchestrates<OrderSubmitted>
{
    public Guid CorrelationId { get; set; }
    public DateTime SubmitTimestamp { get; set; }

    public Task ConsumeAsync(ConsumeContext<OrderSubmitted> context)
    {
        SubmitTimestamp = context.Message.Timestamp;
        return Task.CompletedTask;
    }
}
