using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures and validates Azure Service Bus topic-subscription creation properties.</summary>
public interface IServiceBusSubscriptionConfigurator :
    IServiceBusEndpointEntityConfigurator,
    ISpecification
{
    /// <summary>The path of the subscription's topic.</summary>
    string TopicPath { get; }

    /// <summary>The subscription name, unique per topic.</summary>
    string SubscriptionName { get; }

    /// <summary>Sets the entity path to which active messages are forwarded.</summary>
    string ForwardTo { set; }

    /// <summary>Sets whether filter evaluation failures are dead-lettered.</summary>
    bool? EnableDeadLetteringOnFilterEvaluationExceptions { set; }

    /// <summary>Sets the filter for the subscription's default rule.</summary>
    RuleFilter Filter { set; }

    /// <summary>Sets the complete rule created with the subscription.</summary>
    CreateRuleOptions Rule { set; }

    /// <summary>Projects the configured values into Azure SDK subscription-creation options.</summary>
    /// <returns>The SDK options for creating or comparing the subscription.</returns>
    CreateSubscriptionOptions GetCreateSubscriptionOptions();
}
