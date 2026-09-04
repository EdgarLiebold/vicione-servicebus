using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// The last pipe in a pipeline is always an end pipe that does nothing and returns synchronously
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class LastPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public LastPipe(IFilter<TContext> filter)
    {
        _filter = filter;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _filter.Probe(context);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
