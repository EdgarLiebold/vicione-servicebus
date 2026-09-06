namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures and validates Azure Service Bus topic creation properties.</summary>
public interface IServiceBusTopicConfigurator :
    IServiceBusMessageEntityConfigurator,
    ISpecification
{
    /// <summary>Sets whether the topic supports ordered delivery to subscriptions.</summary>
    bool? SupportOrdering { set; }
}
