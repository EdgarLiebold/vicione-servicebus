using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Completes the AsyncPipeContextAgent when the context is sent to the pipe, and doesn't return until the agent completes.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class AsyncPipeContextFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IAsyncPipeContextAgent<TContext> _agent;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="agent">The agent.</param>
    public AsyncPipeContextFilter(IAsyncPipeContextAgent<TContext> agent)
    {
        _agent = agent;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
    }
}
