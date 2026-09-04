using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a transport ready filter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TransportReadyFilter<T> :
    IFilter<T>
    where T : class, PipeContext
{
    readonly ReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public TransportReadyFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(T context, IPipe<T> next)
    {
        await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

        var agent = new Agent();
        agent.SetReady();

        _context.AddConsumeAgent(agent);

        await next.SendAsync(context).ConfigureAwait(false);

        await agent.Completed.ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("transportReady");
    }
}
