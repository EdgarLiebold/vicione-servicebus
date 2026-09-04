using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a fault dead letter filter implementation.
/// </summary>
public class FaultDeadLetterFilter :
    IFilter<ReceiveContext>
{
    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        throw new MessageNotConsumedException(context.InputAddress, "The message was not consumed");
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("fault-not-consumed");
    }
}
