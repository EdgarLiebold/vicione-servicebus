using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes rethrow error transport pipeline stages.</summary>
public class RethrowErrorTransportFilter :
    IFilter<ExceptionReceiveContext>
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        if (!context.IsFaulted)
            await context.NotifyFaultedAsync(context.Exception).ConfigureAwait(false);

        context.Exception.Rethrow();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("log-fault");
    }
}
