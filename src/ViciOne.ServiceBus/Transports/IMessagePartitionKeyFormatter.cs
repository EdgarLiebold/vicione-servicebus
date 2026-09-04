namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for message partition key formatter.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessagePartitionKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the format partition key operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string FormatPartitionKey(SendContext<TMessage> context);
}
