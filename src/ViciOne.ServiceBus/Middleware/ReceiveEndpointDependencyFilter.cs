using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a receive endpoint dependency filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class ReceiveEndpointDependencyFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly ReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ReceiveEndpointDependencyFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await _context.DependenciesReady.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("receiveEndpointDependencies");
        scope.Add("contextType", typeof(TContext).Name);
    }
}
