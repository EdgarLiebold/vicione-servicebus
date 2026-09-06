using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// A concurrency limit filter that is shared by multiple message types, so that a consumer
/// accepting those various types can be limited to a specific number of consumer instances.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumeConcurrencyLimitFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IConcurrencyLimiter _limiter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="limiter">The limiter.</param>
    public ConsumeConcurrencyLimitFilter(IConcurrencyLimiter limiter)
    {
        _limiter = limiter;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("concurrencyLimit");
        scope.Set(new
        {
            _limiter.Limit,
            _limiter.Available
        });
    }
}
