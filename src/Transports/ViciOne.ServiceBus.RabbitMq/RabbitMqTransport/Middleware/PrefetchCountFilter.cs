using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>Applies the configured RabbitMQ consumer prefetch count to each channel.</summary>
public class PrefetchCountFilter :
    IFilter<ChannelContext>
{
    ushort _prefetchCount;

    /// <summary>Creates a filter with the initial per-consumer prefetch count.</summary>
    /// <param name="prefetchCount">The maximum number of unacknowledged deliveries.</param>
    public PrefetchCountFilter(ushort prefetchCount)
    {
        _prefetchCount = prefetchCount;
    }

    /// <summary>Adds the configured prefetch count to the diagnostic probe.</summary>
    /// <param name="context">The probe context that receives the filter scope.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("prefetchCount");
        scope.Add("prefetchCount", _prefetchCount);
    }

    /// <summary>Applies per-consumer quality of service and invokes the remaining channel pipeline.</summary>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="next">The remainder of the channel pipeline.</param>
    /// <returns>A task that completes with the remaining pipeline.</returns>
    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        await context.BasicQosAsync(0, _prefetchCount, false, context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Updates the value applied when the filter next configures a channel.</summary>
    /// <param name="prefetchCount">The new maximum number of unacknowledged deliveries.</param>
    /// <param name="cancellationToken">Cancellation checked before updating the local setting.</param>
    /// <returns>A task completed after the local value is updated.</returns>
    public Task SetPrefetchCountAsync(ushort prefetchCount, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _prefetchCount = prefetchCount;

        return Task.CompletedTask;
    }
}
