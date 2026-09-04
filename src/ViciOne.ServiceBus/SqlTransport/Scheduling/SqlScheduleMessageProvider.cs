using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.Scheduling;

public class SqlScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly Func<Func<ClientContext, Task>, CancellationToken, Task> _cancel;
    readonly ConsumeContext? _context;
    readonly ISqlHostConfiguration? _hostConfiguration;
    readonly ISendEndpointProvider _sendEndpointProvider;

    public SqlScheduleMessageProvider(ConsumeContext context)
    {
        _context = context;
        _sendEndpointProvider = context;

        _cancel = RetryUsingContextAsync;
    }

    public SqlScheduleMessageProvider(ISqlHostConfiguration hostConfiguration, ISendEndpointProvider sendEndpointProvider)
    {
        _hostConfiguration = hostConfiguration;
        _sendEndpointProvider = sendEndpointProvider;

        _cancel = RetryUsingHostConfigurationAsync;
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var schedulePipe = new ScheduleSendPipe<T>(pipe, dueAt);

        var tokenId = ScheduleTokenIdCache<T>.GetTokenId(message);

        schedulePipe.ScheduledMessageId = tokenId;

        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, schedulePipe, cancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}",
            destinationAddress, schedulePipe.MessageId, TypeCache<T>.ShortName, dueAt, schedulePipe.ScheduledMessageId);

        return new ScheduledMessageHandle<T>(schedulePipe.ScheduledMessageId ?? NewId.NextGuid(), dueAt, destinationAddress, message);
    }

    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return _cancel(async clientContext =>
        {
            var deleted = await clientContext.DeleteScheduledMessageAsync(tokenId, cancellationToken).ConfigureAwait(false);
            if (deleted)
                LogContext.Debug?.Log("CANCEL {TokenId}", tokenId);
        }, cancellationToken);
    }

    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return _cancel(async clientContext =>
        {
            var deleted = await clientContext.DeleteScheduledMessageAsync(tokenId, cancellationToken).ConfigureAwait(false);
            if (deleted)
                LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId}", destinationAddress, tokenId);
        }, cancellationToken);
    }

    Task RetryUsingContextAsync(Func<ClientContext, Task> callback, CancellationToken cancellationToken)
    {
        if (!_context!.TryGetPayload(out ClientContext? clientContext))
            throw new ArgumentException("The client context was not available", nameof(_context));

        return callback(clientContext);
    }

    Task RetryUsingHostConfigurationAsync(Func<ClientContext, Task> callback, CancellationToken cancellationToken)
    {
        ISqlHostConfiguration hostConfiguration = _hostConfiguration!;
        var pipe = new ClientContextPipe(callback, cancellationToken);

        return hostConfiguration.RetryAsync(() => hostConfiguration.ConnectionContextSupervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: hostConfiguration.ConnectionContextSupervisor.Stopping, cancellationToken: cancellationToken);
    }


    class ClientContextPipe :
        IPipe<ConnectionContext>
    {
        readonly Func<ClientContext, Task> _callback;
        readonly CancellationToken _cancellationToken;

        public ClientContextPipe(Func<ClientContext, Task> callback, CancellationToken cancellationToken)
        {
            _callback = callback;
            _cancellationToken = cancellationToken;
        }

        public Task SendAsync(ConnectionContext context)
        {
            var clientContext = context.CreateClientContext(_cancellationToken);

            return _callback(clientContext);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
