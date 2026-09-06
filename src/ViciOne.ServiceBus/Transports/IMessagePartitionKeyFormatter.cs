namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats message partition key values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessagePartitionKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats partition key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The formatted partition key.</returns>
    string FormatPartitionKey(SendContext<TMessage> context);
}
