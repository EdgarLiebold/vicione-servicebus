using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// Completes the AsyncPipeContextAgent when the context is sent to the pipe, and doesn't return until the agent completes
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class AsyncPipeContextFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IAsyncPipeContextAgent<TContext> _agent;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public AsyncPipeContextFilter(IAsyncPipeContextAgent<TContext> agent)
    {
        _agent = agent;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }
}
