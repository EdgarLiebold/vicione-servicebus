using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Configure an Azure Service Bus receive endpoint
/// </summary>
public interface IServiceBusSubscriptionEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IServiceBusEndpointConfigurator
{
    /// <summary>
    /// Specify the filter for the subscription
    /// </summary>
    RuleFilter Filter { set; }

    /// <summary>
    /// Specify a rule for the subscription
    /// </summary>
    CreateRuleOptions Rule { set; }
}
