using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures and validates Azure Service Bus queue creation properties.</summary>
public interface IServiceBusQueueConfigurator :
    IServiceBusMessageEntityConfigurator,
    IServiceBusEndpointEntityConfigurator,
    ISpecification
{
    /// <summary>Sets whether subscription filter evaluation failures are dead-lettered.</summary>
    bool? EnableDeadLetteringOnFilterEvaluationExceptions { set; }

    /// <summary>Sets the entity path to which active messages are forwarded.</summary>
    string ForwardTo { set; }

    /// <summary>Projects the configured values into Azure SDK queue-creation options.</summary>
    /// <returns>The SDK options for creating or comparing the queue.</returns>
    CreateQueueOptions GetCreateQueueOptions();
}
