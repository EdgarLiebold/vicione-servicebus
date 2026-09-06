namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds topology for an Azure Service Bus queue send endpoint.</summary>
public class SendEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    ISendEndpointBrokerTopologyBuilder
{
    /// <summary>Gets or sets the destination queue handle.</summary>
    public QueueHandle Queue { get; set; } = null!;
}
