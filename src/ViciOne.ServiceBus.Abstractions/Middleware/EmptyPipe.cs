using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public class EmptyPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    [DebuggerNonUserCode]
    Task IPipe<TContext>.SendAsync(TContext context)
    {
        return Task.CompletedTask;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
    }
}
