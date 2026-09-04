using ViciOne.ServiceBus.Samples.OrderWorkflow.Contracts;

namespace ViciOne.ServiceBus.Samples.OrderWorkflow.Consumers;

public sealed class SubmitOrderConsumer :
    IConsumer<SubmitOrder>
{
    public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
    {
        await context.Advanced().PublishAsync<OrderSubmitted>(new
        {
            CorrelationId = context.Message.OrderId,
            context.Message.OrderId,
            InVar.Timestamp,
        }, pipe => pipe.Headers.Set("RandomHeader", "RandomValue"));

        if (context.Advanced().IsResponseAccepted<OrderSubmissionAccepted>())
            await context.Advanced().RespondAsync<OrderSubmissionAccepted>(new { context.Message.OrderId });
    }
}
