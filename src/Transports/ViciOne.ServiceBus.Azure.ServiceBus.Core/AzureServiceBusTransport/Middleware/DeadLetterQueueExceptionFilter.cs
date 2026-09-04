using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

/// <summary>
/// Moves a faulted message to the dead-letter queue, rather than the _error queue
/// </summary>
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
