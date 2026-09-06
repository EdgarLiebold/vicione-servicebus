namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Builds receive endpoint broker topology components.</summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public ReceiveEndpointBrokerTopologyBuilder(ReceiveSettings settings)
    {
        Queue = CreateQueue(settings.QueueName, settings.AutoDeleteOnIdle, settings.MaxDeliveryCount);
    }

    /// <summary>Gets the queue.</summary>
    public QueueHandle Queue { get; }
}
