using System;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a sequential endpoint resolver implementation.
/// </summary>
public class SequentialEndpointResolver :
    IRabbitMqEndpointResolver
{
    readonly ClusterNode[] _nodes;
    readonly RabbitMqHostSettings _settings;
    ClusterNode _lastNode;
    int _nextHostIndex;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="nodes">The nodes value.</param>
    /// <param name="settings">The settings value.</param>
    public SequentialEndpointResolver(ClusterNode[] nodes, RabbitMqHostSettings settings)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (nodes.Length == 0)
            throw new ArgumentException("At least one cluster node must be specified", nameof(nodes));

        _nodes = nodes;
        _settings = settings;
        _nextHostIndex = 0;
    }

    /// <summary>
    /// Gets the last host value.
    /// </summary>
    public ClusterNode LastHost => _lastNode;

    /// <summary>
    /// Performs the all operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
