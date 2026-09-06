namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds the queue and subscription topology for one Azure Service Bus receive endpoint.</summary>
public interface IReceiveEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the consuming queue handle.</summary>
    QueueHandle Queue { get; }
}
