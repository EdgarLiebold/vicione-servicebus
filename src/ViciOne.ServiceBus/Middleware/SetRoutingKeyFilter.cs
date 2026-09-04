using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

public class SetRoutingKeyFilter<TMessage> :
    IFilter<SendContext<TMessage>>
    where TMessage : class
{
    readonly IMessageRoutingKeyFormatter<TMessage> _routingKeyFormatter;

    public SetRoutingKeyFilter(IMessageRoutingKeyFormatter<TMessage> routingKeyFormatter)
    {
        _routingKeyFormatter = routingKeyFormatter;
    }

    public Task SendAsync(SendContext<TMessage> context, IPipe<SendContext<TMessage>> next)
    {
        var routingKey = _routingKeyFormatter.FormatRoutingKey(context);

        if (context.TryGetPayload(out RoutingKeySendContext? routingKeySendContext))
            routingKeySendContext.RoutingKey = routingKey;

        return next.SendAsync(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setRoutingKey");
    }
}
