using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a shared connection context implementation.
/// </summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the connection value.
    /// </summary>
    public IConnection Connection => _context.Connection;
    /// <summary>
    /// Gets the description value.
    /// </summary>
    public string Description => _context.Description;
    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => _context.HostAddress;
    /// <summary>
    /// Gets the publisher confirmation value.
    /// </summary>
    public bool PublisherConfirmation => _context.PublisherConfirmation;
    /// <summary>
    /// Gets the batch settings value.
    /// </summary>
    public BatchSettings BatchSettings => _context.BatchSettings;
    /// <summary>
    /// Gets the continuation timeout value.
    /// </summary>
    public TimeSpan ContinuationTimeout => _context.ContinuationTimeout;

    /// <summary>
    /// Gets the stop timeout value.
    /// </summary>
    public TimeSpan StopTimeout => _context.StopTimeout;
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public IRabbitMqBusTopology Topology => _context.Topology;
    /// <summary>
    /// Gets the topology entity cache value.
    /// </summary>
    public RabbitMqTopologyEntityCache TopologyEntityCache => _context.TopologyEntityCache;

    /// <summary>
    /// Creates channel.
    /// </summary>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IChannel> CreateChannelAsync(ushort? concurrentMessageLimit, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateChannelAsync(concurrentMessageLimit, tokenSource.Token).ConfigureAwait(false);
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
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateChannelContextAsync(agent, concurrentMessageLimit, tokenSource.Token).ConfigureAwait(false);
    }
}
