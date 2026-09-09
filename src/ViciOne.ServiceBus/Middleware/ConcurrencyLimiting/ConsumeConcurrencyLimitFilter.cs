using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;

/// <summary>
/// Applies a concurrency budget shared by the consume pipelines configured to use the same limiter.
/// </summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
internal sealed class ConsumeConcurrencyLimitFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IConcurrencyLimiter _limiter;

    /// <summary>Creates a filter that uses the supplied shared limiter.</summary>
    /// <param name="limiter">The owner of the shared concurrency budget.</param>
    public ConsumeConcurrencyLimitFilter(IConcurrencyLimiter limiter)
    {
        _limiter = limiter ?? throw new ArgumentNullException(nameof(limiter));
    }

    /// <summary>Acquires one shared permit, invokes the consumer pipeline, and always returns the permit.</summary>
    /// <param name="context">The consume context whose cancellation stops admission.</param>
    /// <param name="next">The consumer pipeline stage.</param>
    /// <returns>A task that completes with the consumer pipeline stage.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _limiter.WaitAsync(context.CancellationToken).ConfigureAwait(false);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        finally
        {
            _limiter.Release();
        }
    }

    /// <summary>Reports the shared limit and the number of immediately available permits.</summary>
    /// <param name="context">The probe that receives the limiter state.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("concurrencyLimit");
        scope.Set(new
        {
            _limiter.Limit,
            _limiter.Available
        });
    }
}
