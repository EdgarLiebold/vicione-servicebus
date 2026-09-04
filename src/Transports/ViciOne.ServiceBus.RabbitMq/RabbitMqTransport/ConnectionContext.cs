using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// A RabbitMQ connection
/// </summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>
    /// The RabbitMQ Connection
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// The connection description, useful to debug output
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the publisher confirmation value.
    /// </summary>
    bool PublisherConfirmation { get; }

    /// <summary>
    /// Gets the batch settings value.
    /// </summary>
    BatchSettings BatchSettings { get; }

    /// <summary>
    /// Gets the continuation timeout value.
    /// </summary>
    TimeSpan ContinuationTimeout { get; }

    /// <summary>
    /// The time to wait during shutdown of any dependencies before giving up and killing things
    /// </summary>
    TimeSpan StopTimeout { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    IRabbitMqBusTopology Topology { get; }

    /// <summary>
    /// Gets the topology entity cache value.
    /// </summary>
    RabbitMqTopologyEntityCache TopologyEntityCache { get; }

    /// <summary>
    /// Create a channel on the connection
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit used by the operation.</param>
    Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken);

    /// <summary>
    /// Create a channel, and return the <see cref="ChannelContext" />.
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="concurrentMessageLimit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit, CancellationToken cancellationToken);
}
