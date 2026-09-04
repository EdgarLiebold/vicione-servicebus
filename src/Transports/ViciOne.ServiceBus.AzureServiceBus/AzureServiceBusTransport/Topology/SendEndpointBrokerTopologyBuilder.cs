namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a send endpoint broker topology builder implementation.
/// </summary>
public class SendEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    ISendEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// Gets or sets the queue value.
    /// </summary>
    public QueueHandle Queue { get; set; } = null!;
}
