using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Sets the CorrelationId header uses the supplied implementation.
/// </summary>
/// <typeparam name="T">The message type</typeparam>
public class SetCorrelationIdFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly IMessageCorrelationId<T> _messageCorrelationId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    public SetCorrelationIdFilter(IMessageCorrelationId<T> messageCorrelationId)
    {
        _messageCorrelationId = messageCorrelationId;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (_messageCorrelationId.TryGetCorrelationId(context.Message, out var correlationId))
            context.CorrelationId = correlationId;

        return next.SendAsync(context);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SetCorrelationId");
    }
}
