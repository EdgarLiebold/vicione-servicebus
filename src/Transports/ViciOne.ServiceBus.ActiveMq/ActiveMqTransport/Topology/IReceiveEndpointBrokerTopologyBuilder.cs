namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Builds the queue, topics, and consumer bindings required by one ActiveMQ receive endpoint.
/// </summary>
public interface IReceiveEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the receive endpoint's consuming queue.</summary>
    QueueHandle Queue { get; }
}
