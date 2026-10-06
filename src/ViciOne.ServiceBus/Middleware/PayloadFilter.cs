using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes payload pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TPayload">The payload type.</typeparam>
public class PayloadFilter<TContext, TPayload> :
    IFilter<TContext>
    where TContext : class, PipeContext
    where TPayload : class
{
    readonly TPayload _payload;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="payload">The payload.</param>
    public PayloadFilter(TPayload payload)
    {
        _payload = payload;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inline");
    }

    /// <summary>Provides the captured payload when absent, then invokes the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        context.GetOrAddPayload(() => _payload);

        return next.SendAsync(context);
    }
}
