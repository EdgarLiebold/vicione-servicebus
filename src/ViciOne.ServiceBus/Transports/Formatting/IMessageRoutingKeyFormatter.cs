namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats routing keys for a specific message contract.</summary>
/// <typeparam name="TMessage">The message contract whose send context is formatted.</typeparam>
public interface IMessageRoutingKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats the routing key for a message send.</summary>
    /// <param name="context">The message send context.</param>
    /// <returns>The non-null routing key to assign to the transport message.</returns>
    string FormatRoutingKey(SendContext<TMessage> context);
}
