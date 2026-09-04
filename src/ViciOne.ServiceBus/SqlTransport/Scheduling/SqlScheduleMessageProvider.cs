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

        _cancel = RetryUsingContext;
    }

    public SqlScheduleMessageProvider(ISqlHostConfiguration hostConfiguration, ISendEndpointProvider sendEndpointProvider)
    {
        _hostConfiguration = hostConfiguration;
        _sendEndpointProvider = sendEndpointProvider;

        _cancel = RetryUsingHostConfiguration;
    }

    public async Task<ScheduledMessage<T>> ScheduleSend<T>(Uri destinationAddress, DateTime scheduledTime, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var schedulePipe = new ScheduleSendPipe<T>(pipe, scheduledTime);

        var tokenId = ScheduleTokenIdCache<T>.GetTokenId(message);

        schedulePipe.ScheduledMessageId = tokenId;

        var endpoint = await _sendEndpointProvider.GetSendEndpoint(destinationAddress).ConfigureAwait(false);

        await endpoint.Send(message, schedulePipe, cancellationToken).ConfigureAwait(false);

        LogContext.Debug?.Log("SCHED {DestinationAddress} {MessageId} {MessageType} {DeliveryTime:G} {Token}",
            destinationAddress, schedulePipe.MessageId, TypeCache<T>.ShortName, scheduledTime, schedulePipe.ScheduledMessageId);

        return new ScheduledMessageHandle<T>(schedulePipe.ScheduledMessageId ?? NewId.NextGuid(), scheduledTime, destinationAddress, message);
    }

    public Task CancelScheduledSend(Guid tokenId, CancellationToken cancellationToken)
    {
        return _cancel(async clientContext =>
        {
            var deleted = await clientContext.DeleteScheduledMessage(tokenId, cancellationToken).ConfigureAwait(false);
            if (deleted)
                LogContext.Debug?.Log("CANCEL {TokenId}", tokenId);
        }, cancellationToken);
    }

    public Task CancelScheduledSend(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return _cancel(async clientContext =>
        {
            var deleted = await clientContext.DeleteScheduledMessage(tokenId, cancellationToken).ConfigureAwait(false);
            if (deleted)
                LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId}", destinationAddress, tokenId);
        }, cancellationToken);
    }

    Task RetryUsingContext(Func<ClientContext, Task> callback, CancellationToken cancellationToken)
    {
        if (!_context!.TryGetPayload(out ClientContext? clientContext))
            throw new ArgumentException("The client context was not available", nameof(_context));

        return callback(clientContext);
    }

    Task RetryUsingHostConfiguration(Func<ClientContext, Task> callback, CancellationToken cancellationToken)
    {
        ISqlHostConfiguration hostConfiguration = _hostConfiguration!;
        var pipe = new ClientContextPipe(callback, cancellationToken);

        return hostConfiguration.Retry(() => hostConfiguration.ConnectionContextSupervisor.Send(pipe, cancellationToken), cancellationToken,
            hostConfiguration.ConnectionContextSupervisor.Stopping);
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

        public Task Send(ConnectionContext context)
        {
            var clientContext = context.CreateClientContext(_cancellationToken);

            return _callback(clientContext);
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
