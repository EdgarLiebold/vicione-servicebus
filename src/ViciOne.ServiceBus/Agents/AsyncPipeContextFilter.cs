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

    public AsyncPipeContextFilter(IAsyncPipeContextAgent<TContext> agent)
    {
        _agent = agent;
    }

    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await _agent.CreatedAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);

        await _agent.Completed.ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
    {
    }
}
