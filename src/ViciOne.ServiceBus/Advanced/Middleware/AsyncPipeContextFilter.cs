using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Publishes a context before invoking the next stage and waits for the owning agent to complete.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class AsyncPipeContextFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IAsyncPipeContextAgent<TContext> _agent;

    /// <summary>Initializes a filter that publishes through the supplied agent.</summary>
    /// <param name="agent">The agent that receives the context and owns its completion.</param>
    public AsyncPipeContextFilter(IAsyncPipeContextAgent<TContext> agent)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>Publishes the context, invokes the next pipeline stage, and awaits agent completion.</summary>
    /// <param name="context">The context to publish and forward.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that completes after the next stage and the agent lifecycle complete.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    /// <summary>Adds this filter's scope to a probe.</summary>
    /// <param name="context">The probe that receives the filter scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CreateFilterScope("asyncPipeContext");
    }
}
