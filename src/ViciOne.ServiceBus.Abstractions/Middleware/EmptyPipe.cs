using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides an empty pipe implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
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
