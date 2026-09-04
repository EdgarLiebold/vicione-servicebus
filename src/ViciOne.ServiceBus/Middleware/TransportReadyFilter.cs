using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

public class TransportReadyFilter<T> :
    IFilter<T>
    where T : class, PipeContext
{
    readonly ReceiveEndpointContext _context;

    public TransportReadyFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    public async Task SendAsync(T context, IPipe<T> next)
    {
        await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

        var agent = new Agent();
        agent.SetReady();

        _context.AddConsumeAgent(agent);

        await next.SendAsync(context).ConfigureAwait(false);

        await agent.Completed.ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("transportReady");
    }
}
