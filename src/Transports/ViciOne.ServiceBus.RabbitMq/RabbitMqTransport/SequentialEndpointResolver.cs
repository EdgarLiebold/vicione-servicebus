using System;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Cycles through configured RabbitMQ cluster nodes for successive connection attempts.</summary>
public class SequentialEndpointResolver :
    IRabbitMqEndpointResolver
{
    readonly ClusterNode[] _nodes;
    readonly RabbitMqHostSettings _settings;
    ClusterNode _lastNode;
    int _nextHostIndex;

    /// <summary>Creates a round-robin resolver over a nonempty node snapshot.</summary>
    /// <param name="nodes">The RabbitMQ cluster nodes.</param>
    /// <param name="settings">The host defaults and TLS settings applied to resolved endpoints.</param>
    public SequentialEndpointResolver(ClusterNode[] nodes, RabbitMqHostSettings settings)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (nodes.Length == 0)
            throw new ArgumentException("At least one cluster node must be specified", nameof(nodes));

        _nodes = (ClusterNode[])nodes.Clone();
        _settings = settings;
        _nextHostIndex = 0;
    }

    /// <summary>Gets the node selected by the most recent enumeration.</summary>
    public ClusterNode LastHost => _lastNode;

    /// <summary>Selects the next node and returns its RabbitMQ client endpoint.</summary>
    /// <returns>A single endpoint for the current connection attempt.</returns>
    public IEnumerable<AmqpTcpEndpoint> All()
    {
        _lastNode = _nodes[_nextHostIndex % _nodes.Length];

        var hostName = _lastNode.HostName;
        var port = _lastNode.Port ?? _settings.Port;

        Interlocked.Increment(ref _nextHostIndex);

        LogContext.Debug?.Log("Returning next host: {Host}:{Port}", hostName, port);

        var endpoint = new AmqpTcpEndpoint(hostName, port);

        _settings.ApplySslOptions(endpoint.Ssl);

        yield return endpoint;
    }
}
