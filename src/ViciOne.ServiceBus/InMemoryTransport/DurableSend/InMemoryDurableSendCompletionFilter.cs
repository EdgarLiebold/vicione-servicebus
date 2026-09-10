using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.InMemoryTransport.DurableSend;

/// <summary>Completes a volatile durable send only after the full receive pipeline and all receive-owned tasks succeed.</summary>
internal sealed class InMemoryDurableSendCompletionFilter : IFilter<ReceiveContext>
{
    public async Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await next.SendAsync(context).ConfigureAwait(false);

        if (!context.TryGetPayload(out InMemoryDurableSendContext? durableContext))
            return;

        await context.ReceiveCompleted.ConfigureAwait(false);

        if (!context.IsDelivered || context.IsFaulted)
            return;

        await durableContext.ConsumerCompletion.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateFilterScope("inMemoryDurableSendCompletion");
    }
}
