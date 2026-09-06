using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes fault dead letter pipeline stages.</summary>
public class FaultDeadLetterFilter :
    IFilter<ReceiveContext>
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        throw new MessageNotConsumedException(context.InputAddress, "The message was not consumed");
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("fault-not-consumed");
    }
}
