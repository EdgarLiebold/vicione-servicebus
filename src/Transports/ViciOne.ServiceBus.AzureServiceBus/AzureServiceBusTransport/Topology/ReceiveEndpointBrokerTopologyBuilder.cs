namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds topology for an Azure Service Bus queue receive endpoint.</summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>Gets or sets the receive queue handle.</summary>
    public QueueHandle Queue { get; set; } = null!;
}
