using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq connection context implementation.
/// </summary>
public class RabbitMqConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="description">The description value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public RabbitMqConnectionContext(IConnection connection, IRabbitMqHostConfiguration hostConfiguration, string description,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        Connection = connection;

        Description = description;
        HostAddress = hostConfiguration.HostAddress;

        PublisherConfirmation = hostConfiguration.PublisherConfirmation;
        BatchSettings = hostConfiguration.BatchSettings;
        ContinuationTimeout = hostConfiguration.Settings.ContinuationTimeout;

        Topology = hostConfiguration.Topology;
        TopologyEntityCache = new RabbitMqTopologyEntityCache();

        StopTimeout = TimeSpan.FromSeconds(30);

        _lifetime = new TransportLifetime("connection", () => connection.CleanupAsync(200, "Connection Disposed"));
    }

    readonly TransportLifetime _lifetime;

    /// <summary>
    /// The connection's ownership, so the shutdown notification can invalidate it without disposing
    /// a connection that operations are still unwinding out of. Same model as the channel's, and the
    /// same type: a connection that closes underneath a channel being created loses the broker's
    /// reason exactly as a channel did.
    /// </summary>
    internal TransportLifetime Lifetime => _lifetime;

    /// <summary>
    /// Gets the connection value.
    /// </summary>
    public IConnection Connection { get; }

    /// <summary>
    /// Gets the description value.
    /// </summary>
    public string Description { get; }
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress { get; }
    /// <summary>
    /// Gets the publisher confirmation value.
    /// </summary>
    public bool PublisherConfirmation { get; }

    /// <summary>
    /// Gets the batch settings value.
    /// </summary>
    public BatchSettings BatchSettings { get; }
    /// <summary>
    /// Gets the continuation timeout value.
    /// </summary>
    public TimeSpan ContinuationTimeout { get; }

    /// <summary>
    /// Gets the stop timeout value.
    /// </summary>
    public TimeSpan StopTimeout { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public IRabbitMqBusTopology Topology { get; }
    /// <summary>
    /// Gets the topology entity cache value.
    /// </summary>
    public RabbitMqTopologyEntityCache TopologyEntityCache { get; }

    /// <summary>
    /// Creates channel.
    /// </summary>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        var options = new CreateChannelOptions(PublisherConfirmation, PublisherConfirmation, consumerDispatchConcurrency: concurrentMessageLimit);

        var channel = await Connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);

        channel.ContinuationTimeout = ContinuationTimeout;

        return channel;
    }

    /// <summary>
    /// Creates channel context.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        var channel = await CreateChannelAsync(concurrentMessageLimit, cancellationToken).ConfigureAwait(false);

        return new RabbitMqChannelContext(this, channel, agent, cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        TransportLogMessages.DisconnectHost(Description);

        await _lifetime.DisposeAsync().ConfigureAwait(false);

        TransportLogMessages.DisconnectedHost(Description);
    }

    /// <summary>
    /// Takes this connection's lease, or refuses with the reason the connection actually closed for.
    /// </summary>
    TransportLifetime.Lease Lease()
    {
        if (_lifetime.TryLease(out var lease))
            return lease;

        throw _lifetime.NotAvailable();
    }
}
