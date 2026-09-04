namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a receive endpoint broker topology builder implementation.
/// </summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public ReceiveEndpointBrokerTopologyBuilder(ReceiveSettings settings)
    {
        Queue = CreateQueue(settings.QueueName, settings.AutoDeleteOnIdle, settings.MaxDeliveryCount);
    }

    /// <summary>
    /// Gets the queue value.
    /// </summary>
    public QueueHandle Queue { get; }
}
