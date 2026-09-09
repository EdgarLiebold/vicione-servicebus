using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Invokes a pipe, publishes its context, and waits for the owning agent to complete.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class AsyncPipeContextPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IAsyncPipeContextAgent<TContext> _agent;
    readonly IPipe<TContext> _pipe;

    /// <summary>Initializes a pipe that publishes through the supplied agent after its inner pipe completes.</summary>
    /// <param name="agent">The agent that receives the context and owns its completion.</param>
    /// <param name="pipe">The inner pipe to invoke before publishing the context.</param>
    public AsyncPipeContextPipe(IAsyncPipeContextAgent<TContext> agent, IPipe<TContext> pipe)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    /// <summary>Invokes the configured pipe, publishes the context, and awaits agent completion.</summary>
    /// <param name="context">The context to send and publish.</param>
    /// <returns>A task that completes after the inner pipe and the agent lifecycle complete.</returns>
    public async Task SendAsync(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _pipe.SendAsync(context).ConfigureAwait(false);

        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    /// <summary>Adds the inner pipe's diagnostic structure to a probe.</summary>
    /// <param name="context">The probe to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _pipe.Probe(context);
    }
}
