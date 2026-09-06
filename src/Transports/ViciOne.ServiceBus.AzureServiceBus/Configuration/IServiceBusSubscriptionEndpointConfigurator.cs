using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures an Azure Service Bus topic-subscription receive endpoint.</summary>
public interface IServiceBusSubscriptionEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IServiceBusEndpointConfigurator
{
    /// <summary>Sets the filter for the subscription's default rule.</summary>
    RuleFilter Filter { set; }

    /// <summary>Sets the complete rule created with the subscription.</summary>
    CreateRuleOptions Rule { set; }
}
