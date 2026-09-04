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

    public AsyncPipeContextPipe(IAsyncPipeContextAgent<TContext> agent, IPipe<TContext> pipe)
    {
        _agent = agent;
        _pipe = pipe;
    }

    public async Task SendAsync(TContext context)
    {
        await _pipe.SendAsync(context).ConfigureAwait(false);

        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
        _pipe.Probe(context);
    }
}
