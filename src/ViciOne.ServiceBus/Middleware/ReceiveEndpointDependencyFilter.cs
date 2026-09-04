using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

public class ReceiveEndpointDependencyFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly ReceiveEndpointContext _context;

    public ReceiveEndpointDependencyFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    public async Task Send(TContext context, IPipe<TContext> next)
    {
        await _context.DependenciesReady.OrCanceled(context.CancellationToken).ConfigureAwait(false);

        await next.Send(context).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("receiveEndpointDependencies");
        scope.Add("contextType", typeof(TContext).Name);
    }
}
