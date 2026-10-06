using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes receive endpoint dependency pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ReceiveEndpointDependencyFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly ReceiveEndpointContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public ReceiveEndpointDependencyFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    /// <summary>Waits for receive-endpoint dependencies before invoking the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await _context.DependenciesReady.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("receiveEndpointDependencies");
        scope.Add("contextType", typeof(TContext).Name);
    }
}
