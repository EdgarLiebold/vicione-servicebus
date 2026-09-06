namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats message routing key values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageRoutingKeyFormatter<TMessage> :
    IMessageRoutingKeyFormatter<TMessage>
    where TMessage : class
{
    readonly IRoutingKeyFormatter _formatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="formatter">The formatter.</param>
    public MessageRoutingKeyFormatter(IRoutingKeyFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>Formats routing key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The formatted routing key.</returns>
    public string FormatRoutingKey(SendContext<TMessage> context)
    {
        return _formatter.FormatRoutingKey(context);
    }
}
