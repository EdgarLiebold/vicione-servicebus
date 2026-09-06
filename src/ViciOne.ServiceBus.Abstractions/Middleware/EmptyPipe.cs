using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for empty.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
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
