namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Formats provider selector values for messages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageRoutingKeyFormatter<in TMessage>
    where TMessage : class
{
    /// <summary>Formats the provider selector for a message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The formatted provider selector.</returns>
    string FormatRoutingKey(SendContext<TMessage> context);
}
