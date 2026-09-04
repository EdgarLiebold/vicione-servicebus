using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a payload filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TPayload">The t payload type.</typeparam>
public class PayloadFilter<TContext, TPayload> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TPayload : class
{
    readonly TPayload _payload;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="payload">The payload value.</param>
    public PayloadFilter(TPayload payload)
    {
        _payload = payload;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inline");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        context.GetOrAddPayload(() => _payload);

        return next.SendAsync(context);
    }
}
