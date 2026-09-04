namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a receive endpoint broker topology builder implementation.
/// </summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// Gets or sets the queue value.
    /// </summary>
    public QueueHandle Queue { get; set; } = null!;
}
