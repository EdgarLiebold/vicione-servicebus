using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>
/// Prepares a queue for receiving messages using the ReceiveSettings specified.
/// </summary>
public class PrefetchCountFilter :
    IFilter<ChannelContext>
{
    ushort _prefetchCount;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="prefetchCount">The prefetch count value.</param>
    public PrefetchCountFilter(ushort prefetchCount)
    {
        _prefetchCount = prefetchCount;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("prefetchCount");
        scope.Add("prefetchCount", _prefetchCount);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        await context.BasicQosAsync(0, _prefetchCount, false, context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets prefetch count.
    /// </summary>
    /// <param name="prefetchCount">The prefetch count value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetPrefetchCountAsync(ushort prefetchCount, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _prefetchCount = prefetchCount;

        return Task.CompletedTask;
    }
}
