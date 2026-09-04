using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public class InlineFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly InlineFilterMethod<TContext> _filterMethod;

    public InlineFilter(InlineFilterMethod<TContext> filterMethod)
    {
        _filterMethod = filterMethod;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("inline");
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        return _filterMethod(context, next);
    }
}
