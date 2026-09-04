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

    public SetCorrelationIdFilter(IMessageCorrelationId<T> messageCorrelationId)
    {
        _messageCorrelationId = messageCorrelationId;
    }

    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        if (_messageCorrelationId.TryGetCorrelationId(context.Message, out var correlationId))
            context.CorrelationId = correlationId;

        return next.SendAsync(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SetCorrelationId");
    }
}
