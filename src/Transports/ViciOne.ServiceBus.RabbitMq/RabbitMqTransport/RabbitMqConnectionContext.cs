using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Owns one RabbitMQ connection and leases it to channel-creation operations until disposal.</summary>
public class RabbitMqConnectionContext :
    BasePipeContext,
    ConnectionContext,
    IAsyncDisposable
{
    /// <summary>Creates a lifetime-managed context around an open RabbitMQ connection.</summary>
    /// <param name="connection">The RabbitMQ client connection.</param>
    /// <param name="hostConfiguration">The effective host and topology configuration.</param>
    /// <param name="description">The sanitized connection description used in diagnostics.</param>
    /// <param name="cancellationToken">Cancellation linked to the connection context.</param>
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
    /// Coordinates invalidation and disposal so shutdown preserves the broker reason while active
    /// connection operations retain their lease until completion.
    /// </summary>
    internal TransportLifetime Lifetime => _lifetime;

    /// <summary>Gets the RabbitMQ client connection.</summary>
    public IConnection Connection { get; }

    /// <summary>Gets the sanitized connection description.</summary>
    public string Description { get; }
    /// <summary>Gets the host address.</summary>
    public Uri HostAddress { get; }
    /// <summary>Gets whether channels use RabbitMQ publisher confirmations.</summary>
    public bool PublisherConfirmation { get; }

    /// <summary>Gets client-side publish-batch settings.</summary>
    public BatchSettings BatchSettings { get; }
    /// <summary>Gets the timeout for RabbitMQ client RPC continuations.</summary>
    public TimeSpan ContinuationTimeout { get; }

    /// <summary>Gets the maximum time allowed for dependent transport agents to stop.</summary>
    public TimeSpan StopTimeout { get; }

    /// <summary>Gets bus-level RabbitMQ topology.</summary>
    public IRabbitMqBusTopology Topology { get; }
    /// <summary>Gets the per-connection cache of successfully declared broker entities.</summary>
    public RabbitMqTopologyEntityCache TopologyEntityCache { get; }

    /// <summary>Creates and configures a RabbitMQ channel under a connection lease.</summary>
    /// <param name="concurrentMessageLimit">The optional consumer dispatch concurrency.</param>
    /// <param name="cancellationToken">Cancellation for channel creation.</param>
    /// <returns>The open RabbitMQ channel.</returns>
    public async Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        var options = new CreateChannelOptions(PublisherConfirmation, PublisherConfirmation, consumerDispatchConcurrency: concurrentMessageLimit);

        var channel = await Connection.CreateChannelAsync(options, cancellationToken).ConfigureAwait(false);

        try
        {
            channel.ContinuationTimeout = ContinuationTimeout;
            return channel;
        }
        catch
        {
            await channel.CleanupAsync(200, "Channel initialization failed").ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Creates a channel and wraps it in a lifetime-managed context.</summary>
    /// <param name="agent">The transport agent that owns the channel.</param>
    /// <param name="concurrentMessageLimit">The optional consumer dispatch concurrency.</param>
    /// <param name="cancellationToken">Cancellation for channel creation and the resulting context.</param>
    /// <returns>The active RabbitMQ channel context.</returns>
    public async Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var lease = Lease();

        var channel = await CreateChannelAsync(concurrentMessageLimit, cancellationToken).ConfigureAwait(false);

        return new RabbitMqChannelContext(this, channel, agent, cancellationToken);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task-like value that completes after in-flight leases finish and the connection is cleaned up.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            TransportLogMessages.DisconnectHost(Description);
        }
        catch (Exception)
        {
            // Diagnostics do not prevent owned connection cleanup.
        }

        await _lifetime.DisposeAsync().ConfigureAwait(false);

        try
        {
            TransportLogMessages.DisconnectedHost(Description);
        }
        catch (Exception)
        {
            // Diagnostics do not change the completed cleanup outcome.
        }
    }

    /// <summary>Takes this connection's lease, or refuses with the reason the connection actually closed for.</summary>
    /// <returns>A lease that prevents connection disposal until released.</returns>
    TransportLifetime.Lease Lease()
    {
        if (_lifetime.TryLease(out var lease))
            return lease;

        throw _lifetime.NotAvailable();
    }
}
