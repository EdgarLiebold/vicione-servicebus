using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public class FilterPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext> _filter;
    readonly IPipe<TContext> _next;

    public FilterPipe(IFilter<TContext> filter, IPipe<TContext> next)
    {
        _filter = filter;
        _next = next;
    }

    public void Probe(ProbeContext context)
    {
        _filter.Probe(context);
        _next.Probe(context);
    }

    [DebuggerStepThrough]
    public Task SendAsync(TContext context)
    {
        return _filter.SendAsync(context, _next);
    }
}
