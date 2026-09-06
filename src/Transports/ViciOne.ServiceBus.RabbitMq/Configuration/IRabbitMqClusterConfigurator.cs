namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ cluster nodes used for connection failover.</summary>
public interface IRabbitMqClusterConfigurator
{
    /// <summary>Adds a cluster node address containing a host name and an optional port.</summary>
    /// <param name="nodeAddress">The node address in <c>host</c> or <c>host:port</c> form.</param>
    void Node(string nodeAddress);
}
