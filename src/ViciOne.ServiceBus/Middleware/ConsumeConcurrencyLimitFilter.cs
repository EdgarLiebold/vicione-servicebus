using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// A concurrency limit filter that is shared by multiple message types, so that a consumer
/// accepting those various types can be limited to a specific number of consumer instances.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class ConsumeConcurrencyLimitFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IConcurrencyLimiter _limiter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="limiter">The limiter value.</param>
    public ConsumeConcurrencyLimitFilter(IConcurrencyLimiter limiter)
    {
        _limiter = limiter;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
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
