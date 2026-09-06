namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures the Azure Service Bus topic used to publish a message contract.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IServiceBusMessagePublishTopologyConfigurator<TMessage> :
    IServiceBusMessagePublishTopologyConfigurator,
    IMessagePublishTopologyConfigurator<TMessage>,
    IServiceBusMessagePublishTopology<TMessage>
    where TMessage : class
{
}


/// <summary>Configures a runtime-typed Azure Service Bus publish topic.</summary>
public interface IServiceBusMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IServiceBusTopicConfigurator
{
}
