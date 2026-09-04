using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMqTransport.Middleware;

/// <summary>
/// Prepares a queue for receiving messages using the ReceiveSettings specified.
/// </summary>
public class PrefetchCountFilter :
    IFilter<ChannelContext>
{
    ushort _prefetchCount;

    public PrefetchCountFilter(ushort prefetchCount)
    {
        _prefetchCount = prefetchCount;
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("prefetchCount");
        scope.Add("prefetchCount", _prefetchCount);
    }

    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        await context.BasicQosAsync(0, _prefetchCount, false, context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    public Task SetPrefetchCountAsync(ushort prefetchCount, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _prefetchCount = prefetchCount;

        return Task.CompletedTask;
    }
}
