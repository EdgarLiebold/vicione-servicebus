using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Dead-letters an unconsumed delivery in Azure Service Bus instead of forwarding it to the transport skipped queue.</summary>
public class DeadLetterQueueFilter :
    IFilter<ReceiveContext>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("dead-letter-queue");
    }

    async Task IFilter<ReceiveContext>.SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        if (!context.TryGetPayload(out MessageLockContext? lockContext))
            throw new TransportException(context.InputAddress, $"The {nameof(MessageLockContext)} was not available on the {nameof(ReceiveContext)}.");

        await lockContext.DeadLetterAsync().ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }
}
