using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq cluster configurator implementation.
/// </summary>
public class RabbitMqClusterConfigurator :
    IRabbitMqClusterConfigurator
{
    readonly List<ClusterNode> _nodes;
    readonly RabbitMqHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public RabbitMqClusterConfigurator(RabbitMqHostSettings settings)
    {
        _settings = settings;
        _nodes = new List<ClusterNode>();
    }

    /// <summary>
    /// Gets the cluster members value.
    /// </summary>
    public ClusterNode[] ClusterMembers => _nodes.ToArray();

    /// <summary>
    /// Performs the node operation.
    /// </summary>
    /// <param name="nodeAddress">The node address value.</param>
    public void Node(string nodeAddress)
    {
        _nodes.Add(ClusterNode.Parse(nodeAddress));
    }

    /// <summary>
    /// Gets endpoint resolver.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IRabbitMqEndpointResolver? GetEndpointResolver()
    {
        if (_nodes.Count <= 0)
            return null;

        return new SequentialEndpointResolver(ClusterMembers, _settings);
    }
}
