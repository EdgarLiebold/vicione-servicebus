namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Sagas;

using Contracts;

public sealed class OrderDeliverySaga :
    ISaga,
    InitiatedByOrOrchestrates<OrderSubmitted>
{
    public Guid CorrelationId { get; set; }
    public DateTime SubmitTimestamp { get; set; }

    public Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        SubmitTimestamp = context.Message.Timestamp;
        return Task.CompletedTask;
    }
}
