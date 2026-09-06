using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes a RabbitMQ connection and the transport settings shared by its channels.</summary>
public interface ConnectionContext :
    PipeContext
{
    /// <summary>Gets the RabbitMQ client connection.</summary>
    IConnection Connection { get; }

    /// <summary>Gets the sanitized connection description used in diagnostics.</summary>
    string Description { get; }

    /// <summary>Gets the host address.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets whether channels use RabbitMQ publisher confirmations.</summary>
    bool PublisherConfirmation { get; }

    /// <summary>Gets client-side publish-batch settings.</summary>
    BatchSettings BatchSettings { get; }

    /// <summary>Gets the timeout for RabbitMQ client RPC continuations.</summary>
    TimeSpan ContinuationTimeout { get; }

    /// <summary>Gets the maximum time allowed for dependent transport agents to stop.</summary>
    TimeSpan StopTimeout { get; }

    /// <summary>Gets bus-level RabbitMQ topology.</summary>
    IRabbitMqBusTopology Topology { get; }

    /// <summary>Gets the per-connection cache of successfully declared broker entities.</summary>
    RabbitMqTopologyEntityCache TopologyEntityCache { get; }

    /// <summary>Creates and configures a RabbitMQ channel on this connection.</summary>
    /// <param name="concurrentMessageLimit">The optional consumer concurrency used to size prefetch.</param>
    /// <param name="cancellationToken">Cancellation for channel creation.</param>
    /// <returns>The open RabbitMQ channel.</returns>
    Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken);

    /// <summary>Creates a channel and wraps it in a lifetime-managed <see cref="ChannelContext" />.</summary>
    /// <param name="agent">The transport agent that owns the channel.</param>
    /// <param name="concurrentMessageLimit">The optional consumer concurrency used to size prefetch.</param>
    /// <param name="cancellationToken">Cancellation for channel creation.</param>
    /// <returns>The active RabbitMQ channel context.</returns>
    Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit, CancellationToken cancellationToken);
}
