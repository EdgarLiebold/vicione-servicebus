using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Leases a send-endpoint context and links each send operation to the lease lifetime.</summary>
public class SharedSendEndpointContext :
    ProxyPipeContext,
    SendEndpointContext
{
    readonly SendEndpointContext _context;

    /// <summary>Initializes a lease over an existing send-endpoint context.</summary>
    /// <param name="context">The shared send-endpoint context.</param>
    /// <param name="cancellationToken">The token that bounds this lease.</param>
    public SharedSendEndpointContext(SendEndpointContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token that bounds this lease.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the namespace connection that owns the sender.</summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>Gets the destination entity path.</summary>
    public string EntityPath => _context.EntityPath;

    /// <summary>Sends a message using cancellation linked to this endpoint lease.</summary>
    /// <param name="message">The Azure Service Bus message to send.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that completes when the SDK send completes.</returns>
    public async Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.SendAsync(message, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Schedules a message using cancellation linked to this endpoint lease.</summary>
    /// <param name="message">The Azure Service Bus message to schedule.</param>
    /// <param name="scheduleEnqueueTimeUtc">The UTC enqueue time.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that produces the broker sequence number.</returns>
    public async Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.ScheduleSendAsync(message, scheduleEnqueueTimeUtc, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>Cancels a scheduled message using cancellation linked to this endpoint lease.</summary>
    /// <param name="sequenceNumber">The broker sequence number returned by scheduling.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>A task that completes when the broker accepts the cancellation.</returns>
    public async Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.CancelScheduledSendAsync(sequenceNumber, tokenSource.Token).ConfigureAwait(false);
    }
}
