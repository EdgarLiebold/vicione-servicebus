using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a shared send endpoint context implementation.
/// </summary>
public class SharedSendEndpointContext :
    ProxyPipeContext,
    SendEndpointContext
{
    readonly SendEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedSendEndpointContext(SendEndpointContext context, CancellationToken cancellationToken)
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
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>
    /// Gets the entity path value.
    /// </summary>
    public string EntityPath => _context.EntityPath;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.SendAsync(message, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="scheduleEnqueueTimeUtc">The schedule enqueue time utc value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.ScheduleSendAsync(message, scheduleEnqueueTimeUtc, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.CancelScheduledSendAsync(sequenceNumber, tokenSource.Token).ConfigureAwait(false);
    }
}
