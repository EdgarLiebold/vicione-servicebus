namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus send conventions for a message contract.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public interface IServiceBusMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IServiceBusMessageSendTopology<TMessage>,
    IServiceBusMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures runtime-typed Azure Service Bus send conventions.</summary>
public interface IServiceBusMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
