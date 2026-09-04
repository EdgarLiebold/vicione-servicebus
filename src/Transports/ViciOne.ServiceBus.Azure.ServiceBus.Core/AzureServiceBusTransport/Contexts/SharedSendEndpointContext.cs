using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class SharedSendEndpointContext :
    ProxyPipeContext,
    SendEndpointContext
{
    readonly SendEndpointContext _context;

    public SharedSendEndpointContext(SendEndpointContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    public string EntityPath => _context.EntityPath;

    public async Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.SendAsync(message, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<long> ScheduleSendAsync(ServiceBusMessage message, DateTimeOffset scheduleEnqueueTimeUtc, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.ScheduleSendAsync(message, scheduleEnqueueTimeUtc, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task CancelScheduledSendAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.CancelScheduledSendAsync(sequenceNumber, tokenSource.Token).ConfigureAwait(false);
    }
}
