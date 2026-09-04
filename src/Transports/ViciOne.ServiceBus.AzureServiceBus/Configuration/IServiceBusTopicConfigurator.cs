namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus topic configurator.
/// </summary>
public interface IServiceBusTopicConfigurator :
    IServiceBusMessageEntityConfigurator,
    ISpecification
{
    /// <summary>
    /// If True, the topic will deliver messages to subscriptions in order
    /// </summary>
    bool? SupportOrdering { set; }
}
