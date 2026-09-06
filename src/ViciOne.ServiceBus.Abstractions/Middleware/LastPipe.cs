using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>The last pipe in a pipeline is always an end pipe that does nothing and returns synchronously.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class LastPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public LastPipe(IFilter<TContext> filter)
    {
        _filter = filter;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _filter.Probe(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerStepThrough]
    public Task SendAsync(TContext context)
    {
        return _filter.SendAsync(context, Cache.LastPipe);
    }


    static class Cache
    {
        internal static readonly IPipe<TContext> LastPipe = new Last();
    }


    class Last :
        IPipe<TContext>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context)
        {
            return Task.CompletedTask;
        }
    }
}
