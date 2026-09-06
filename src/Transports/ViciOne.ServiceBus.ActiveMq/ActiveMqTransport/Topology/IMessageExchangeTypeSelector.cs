namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Selects the provider destination type for a published message.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IMessageExchangeTypeSelector<in TMessage>
    where TMessage : class
{
    /// <summary>Gets the default provider destination type.</summary>
    string DefaultExchangeType { get; }

    /// <summary>Returns the provider destination type for the named topic.</summary>
    /// <param name="exchangeName">The topic name.</param>
    /// <returns>The provider destination type used for sending.</returns>
    string GetExchangeType(string exchangeName);
}
