using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Collects RabbitMQ cluster nodes and creates their sequential endpoint resolver.</summary>
public class RabbitMqClusterConfigurator :
    IRabbitMqClusterConfigurator
{
    readonly List<ClusterNode> _nodes;
    readonly RabbitMqHostSettings _settings;

    /// <summary>Creates a cluster configurator using the host's default port settings.</summary>
    /// <param name="settings">The RabbitMQ host settings shared by cluster nodes.</param>
    public RabbitMqClusterConfigurator(RabbitMqHostSettings settings)
    {
        _settings = settings;
        _nodes = new List<ClusterNode>();
    }

    /// <summary>Gets the cluster members.</summary>
    public ClusterNode[] ClusterMembers => _nodes.ToArray();

    /// <summary>Parses and adds a cluster node.</summary>
    /// <param name="nodeAddress">The node in <c>host</c> or <c>host:port</c> form.</param>
    public void Node(string nodeAddress)
    {
        _nodes.Add(ClusterNode.Parse(nodeAddress));
    }

    /// <summary>Creates a sequential resolver when at least one cluster node was configured.</summary>
    /// <returns>The resolver, or <see langword="null" /> when no nodes were added.</returns>
    public IRabbitMqEndpointResolver? GetEndpointResolver()
    {
        if (_nodes.Count <= 0)
            return null;

        return new SequentialEndpointResolver(ClusterMembers, _settings);
    }
}
