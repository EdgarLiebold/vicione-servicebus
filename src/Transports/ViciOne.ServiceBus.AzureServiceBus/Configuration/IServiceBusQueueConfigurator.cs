using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus queue configurator.
/// </summary>
public interface IServiceBusQueueConfigurator :
    IServiceBusMessageEntityConfigurator,
    IServiceBusEndpointEntityConfigurator,
    ISpecification
{
    /// <summary>
    /// Move messages to the dead letter queue on filter evaluation exception
    /// </summary>
    bool? EnableDeadLetteringOnFilterEvaluationExceptions { set; }

    /// <summary>
    /// Sets the path where messages are forwarded to
    /// </summary>
    string ForwardTo { set; }

    /// <summary>
    /// Create the CreateQueueOptions for the configuration
    /// </summary>
    /// <returns></returns>
    CreateQueueOptions GetCreateQueueOptions();
}
