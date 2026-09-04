using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a rethrow error transport filter implementation.
/// </summary>
public class RethrowErrorTransportFilter :
    IFilter<ExceptionReceiveContext>
{
    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        if (!context.IsFaulted)
            await context.NotifyFaultedAsync(context.Exception).ConfigureAwait(false);

        context.Exception.Rethrow();
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("log-fault");
    }
}
