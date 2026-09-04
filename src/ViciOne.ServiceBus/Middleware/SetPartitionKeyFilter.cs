using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a set partition key filter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SetPartitionKeyFilter<TMessage> :
    IFilter<SendContext<TMessage>>
    where TMessage : class
{
    readonly IMessagePartitionKeyFormatter<TMessage> _routingKeyFormatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="routingKeyFormatter">The routing key formatter value.</param>
    public SetPartitionKeyFilter(IMessagePartitionKeyFormatter<TMessage> routingKeyFormatter)
    {
        _routingKeyFormatter = routingKeyFormatter;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<TMessage> context, IPipe<SendContext<TMessage>> next)
    {
        var routingKey = _routingKeyFormatter.FormatPartitionKey(context);

        if (context.TryGetPayload(out PartitionKeySendContext? routingKeySendContext))
            routingKeySendContext.PartitionKey = routingKey;

        return next.SendAsync(context);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setPartitionKey");
    }
}
