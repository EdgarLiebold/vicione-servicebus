using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Sets the CorrelationId header uses the supplied implementation.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class SetCorrelationIdFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly IMessageCorrelationId<T> _messageCorrelationId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageCorrelationId">The message correlation id.</param>
    public SetCorrelationIdFilter(IMessageCorrelationId<T> messageCorrelationId)
    {
        _messageCorrelationId = messageCorrelationId;
    }

    /// <summary>Applies an available correlation identifier before invoking the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (_messageCorrelationId.TryGetCorrelationId(context.Message, out var correlationId))
            context.CorrelationId = correlationId;

        return next.SendAsync(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SetCorrelationId");
    }
}
