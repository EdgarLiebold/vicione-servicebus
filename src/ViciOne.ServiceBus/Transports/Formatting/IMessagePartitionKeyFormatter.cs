namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats partition keys for a specific message contract.</summary>
/// <typeparam name="TMessage">The message contract whose send context is formatted.</typeparam>
public interface IMessagePartitionKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats the partition key for a message send.</summary>
    /// <param name="context">The message send context.</param>
    /// <returns>The non-null partition key to assign to the transport message.</returns>
    string FormatPartitionKey(SendContext<TMessage> context);
}
