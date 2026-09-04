using ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Consumers;

public sealed class SubmitOrderConsumer :
    IConsumer<SubmitOrder>
{
    public async Task Consume(ConsumeContext<SubmitOrder> context)
    {
        await context.Publish<OrderSubmitted>(new
        {
            CorrelationId = context.Message.OrderId,
            context.Message.OrderId,
            InVar.Timestamp,
        }, pipe => pipe.Headers.Set("RandomHeader", "RandomValue"));

        if (context.IsResponseAccepted<OrderSubmissionAccepted>())
            await context.RespondAsync<OrderSubmissionAccepted>(new { context.Message.OrderId });
    }
}
