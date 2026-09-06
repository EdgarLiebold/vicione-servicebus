namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats message routing key values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageRoutingKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats routing key.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The formatted routing key.</returns>
    string FormatRoutingKey(SendContext<TMessage> context);
}
