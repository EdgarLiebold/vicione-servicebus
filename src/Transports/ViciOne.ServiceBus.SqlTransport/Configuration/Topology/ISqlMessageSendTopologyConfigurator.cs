namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql message send topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISqlMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    ISqlMessageSendTopology<TMessage>,
    ISqlMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures sql message send topology.</summary>
public interface ISqlMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
