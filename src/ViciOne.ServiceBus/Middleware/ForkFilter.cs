using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Forks a single pipe into two pipes, which are executed concurrently.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ForkFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IPipe<TContext> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public ForkFilter(IPipe<TContext> pipe)
    {
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        Task? pipeTask = _pipe.SendAsync(context);
        Task? nextTask = null;
        try
        {
            nextTask = next.SendAsync(context);
            return Task.WhenAll(pipeTask!, nextTask!);
        }
        catch (Exception exception)
        {
            return Task.WhenAll(
                pipeTask ?? Task.CompletedTask,
                nextTask ?? Task.CompletedTask,
                Task.FromException(exception));
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("fork");
        _pipe.Probe(scope);
    }
}
