using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Delegates connection operations to a shared connection while combining the connection lease and caller cancellation tokens.</summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>Creates a cancellable view over an existing connection context.</summary>
    /// <param name="context">The underlying connection context.</param>
    /// <param name="cancellationToken">The token that ends this shared-connection lease.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token that ends this shared-connection lease.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the underlying RabbitMQ connection.</summary>
    public IConnection Connection => _context.Connection;
    /// <summary>Gets the human-readable connection description.</summary>
    public string Description => _context.Description;
    /// <summary>Gets the RabbitMQ host address.</summary>
    public Uri HostAddress => _context.HostAddress;
    /// <summary>Gets whether channels created by this connection use publisher confirmations.</summary>
    public bool PublisherConfirmation => _context.PublisherConfirmation;
    /// <summary>Gets the publish-batching settings.</summary>
    public BatchSettings BatchSettings => _context.BatchSettings;
    /// <summary>Gets the timeout used for RabbitMQ client continuations.</summary>
    public TimeSpan ContinuationTimeout => _context.ContinuationTimeout;

    /// <summary>Gets the time allowed for transport shutdown.</summary>
    public TimeSpan StopTimeout => _context.StopTimeout;
    /// <summary>Gets the RabbitMQ bus topology.</summary>
    public IRabbitMqBusTopology Topology => _context.Topology;
    /// <summary>Gets the cache that coalesces topology declarations on this connection.</summary>
    public RabbitMqTopologyEntityCache TopologyEntityCache => _context.TopologyEntityCache;

    /// <summary>Creates a RabbitMQ channel through the underlying connection context.</summary>
    /// <param name="concurrentMessageLimit">The optional publisher-confirm concurrency limit.</param>
    /// <param name="cancellationToken">The token that cancels channel creation in addition to the shared-connection token.</param>
    /// <returns>The created RabbitMQ channel.</returns>
    public async Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateChannelAsync(concurrentMessageLimit, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Creates a supervised channel context through the underlying connection context.</summary>
    /// <param name="agent">The agent that owns the channel context lifetime.</param>
    /// <param name="concurrentMessageLimit">The optional publisher-confirm concurrency limit.</param>
    /// <param name="cancellationToken">The token that cancels channel creation in addition to the shared-connection token.</param>
    /// <returns>The created channel context.</returns>
    public async Task<ChannelContext> CreateChannelContextAsync(IAgent agent, ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateChannelContextAsync(agent, concurrentMessageLimit, tokenSource.Token).ConfigureAwait(false);
    }
}
