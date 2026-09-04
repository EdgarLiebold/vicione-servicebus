namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Defines the contract for message routing key formatter.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageRoutingKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the format routing key operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string FormatRoutingKey(SendContext<TMessage> context);
}
