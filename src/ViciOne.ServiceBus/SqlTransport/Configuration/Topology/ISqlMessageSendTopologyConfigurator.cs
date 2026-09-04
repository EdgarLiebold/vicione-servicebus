namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message send topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISqlMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    ISqlMessageSendTopology<TMessage>,
    ISqlMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for sql message send topology configurator.
/// </summary>
public interface ISqlMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
