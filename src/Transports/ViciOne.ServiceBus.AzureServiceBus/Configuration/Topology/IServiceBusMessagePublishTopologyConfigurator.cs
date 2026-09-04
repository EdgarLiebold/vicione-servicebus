namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus message publish topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IServiceBusMessagePublishTopologyConfigurator<TMessage> :
    IServiceBusMessagePublishTopologyConfigurator,
    IMessagePublishTopologyConfigurator<TMessage>,
    IServiceBusMessagePublishTopology<TMessage>
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for service bus message publish topology configurator.
/// </summary>
public interface IServiceBusMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IServiceBusTopicConfigurator
{
}
