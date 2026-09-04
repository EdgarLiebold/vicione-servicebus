using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// Completes the AsyncPipeContextAgent when the context is sent to the pipe, and doesn't return until the agent completes
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class AsyncPipeContextPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IAsyncPipeContextAgent<TContext> _agent;
    readonly IPipe<TContext> _pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    /// <param name="pipe">The pipe value.</param>
    public AsyncPipeContextPipe(IAsyncPipeContextAgent<TContext> agent, IPipe<TContext> pipe)
    {
        _agent = agent;
        _pipe = pipe;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(TContext context)
    {
        await _pipe.SendAsync(context).ConfigureAwait(false);

        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _pipe.Probe(context);
    }
}
