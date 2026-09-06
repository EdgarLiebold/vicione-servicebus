using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Dead-letters a faulted delivery in Azure Service Bus instead of forwarding it to the transport error queue.</summary>
public class DeadLetterQueueExceptionFilter :
    IFilter<ExceptionReceiveContext>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("dead-letter-queue");
    }

    async Task IFilter<ExceptionReceiveContext>.SendAsync(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        if (!context.TryGetPayload(out MessageLockContext? lockContext))
            throw new TransportException(context.InputAddress, $"The {nameof(MessageLockContext)} was not available on the {nameof(ReceiveContext)}.");

        await lockContext.DeadLetterAsync(context.Exception).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }
}
