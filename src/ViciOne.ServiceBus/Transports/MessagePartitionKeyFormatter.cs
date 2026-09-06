namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats message partition key values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessagePartitionKeyFormatter<TMessage> :
    IMessagePartitionKeyFormatter<TMessage>
    where TMessage : class
{
    readonly IPartitionKeyFormatter _formatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="formatter">The formatter.</param>
    public MessagePartitionKeyFormatter(IPartitionKeyFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>Formats partition key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The formatted partition key.</returns>
    public string FormatPartitionKey(SendContext<TMessage> context)
    {
        return _formatter.FormatPartitionKey(context);
    }
}
