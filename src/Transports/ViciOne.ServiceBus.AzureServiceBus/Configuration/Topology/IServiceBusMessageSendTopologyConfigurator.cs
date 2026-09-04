namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus message send topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IServiceBusMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IServiceBusMessageSendTopology<TMessage>,
    IServiceBusMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for service bus message send topology configurator.
/// </summary>
public interface IServiceBusMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
