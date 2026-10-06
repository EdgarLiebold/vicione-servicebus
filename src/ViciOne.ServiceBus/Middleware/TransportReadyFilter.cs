using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes transport ready pipeline stages.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class TransportReadyFilter<T> :
    IFilter<T>
    where T : class, PipeContext
{
    readonly ReceiveEndpointContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public TransportReadyFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    /// <summary>Notifies transport readiness, registers an agent, runs the continuation and awaits agent completion.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(T context, IPipe<T> next)
    {
        await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

        var agent = new Agent();
        agent.SetReady();

        _context.AddConsumeAgent(agent);

        await next.SendAsync(context).ConfigureAwait(false);

        await agent.Completed.ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("transportReady");
    }
}
