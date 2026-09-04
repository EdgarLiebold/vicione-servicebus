namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a message partition key formatter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessagePartitionKeyFormatter<TMessage> :
    IMessagePartitionKeyFormatter<TMessage>
    where TMessage : class
{
    readonly IPartitionKeyFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public MessagePartitionKeyFormatter(IPartitionKeyFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// Performs the format partition key operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public string FormatPartitionKey(SendContext<TMessage> context)
    {
        return _formatter.FormatPartitionKey(context);
    }
}
