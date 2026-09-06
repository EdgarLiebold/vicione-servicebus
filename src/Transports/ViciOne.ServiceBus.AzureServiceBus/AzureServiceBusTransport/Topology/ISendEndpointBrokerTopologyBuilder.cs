namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds the queue topology required by an Azure Service Bus send endpoint.</summary>
public interface ISendEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets or sets the destination queue handle.</summary>
    QueueHandle Queue { get; set; }
}
