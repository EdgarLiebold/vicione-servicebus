using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

public class SetPartitionKeyFilter<TMessage> :
    IFilter<SendContext<TMessage>>
    where TMessage : class
{
    readonly IMessagePartitionKeyFormatter<TMessage> _routingKeyFormatter;

    public SetPartitionKeyFilter(IMessagePartitionKeyFormatter<TMessage> routingKeyFormatter)
    {
        _routingKeyFormatter = routingKeyFormatter;
    }

    public Task SendAsync(SendContext<TMessage> context, IPipe<SendContext<TMessage>> next)
    {
        var routingKey = _routingKeyFormatter.FormatPartitionKey(context);

        if (context.TryGetPayload(out PartitionKeySendContext? routingKeySendContext))
            routingKeySendContext.PartitionKey = routingKey;

        return next.SendAsync(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setPartitionKey");
    }
}
