using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes set partition key pipeline stages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SetPartitionKeyFilter<TMessage> :
    IFilter<SendContext<TMessage>>
    where TMessage : class
{
    readonly IMessagePartitionKeyFormatter<TMessage> _routingKeyFormatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="routingKeyFormatter">The routing key formatter.</param>
    public SetPartitionKeyFilter(IMessagePartitionKeyFormatter<TMessage> routingKeyFormatter)
    {
        _routingKeyFormatter = routingKeyFormatter;
    }

    /// <summary>Applies the formatted partition key to an available transport payload before invoking the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<TMessage> context, IPipe<SendContext<TMessage>> next)
    {
        var routingKey = _routingKeyFormatter.FormatPartitionKey(context);

        if (context.TryGetPayload(out PartitionKeySendContext? routingKeySendContext))
            routingKeySendContext.PartitionKey = routingKey;

        return next.SendAsync(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setPartitionKey");
    }
}
