using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Provides sql schedule message services.</summary>
public class SqlScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly Func<Func<ClientContext, Task>, CancellationToken, Task> _cancel;
    readonly ConsumeContext? _context;
    readonly ISqlHostConfiguration? _hostConfiguration;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public SqlScheduleMessageProvider(ConsumeContext context)
    {
        _context = context;
        _sendEndpointProvider = context;

        _cancel = RetryUsingContextAsync;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    public SqlScheduleMessageProvider(ISqlHostConfiguration hostConfiguration, ISendEndpointProvider sendEndpointProvider)
    {
        _hostConfiguration = hostConfiguration;
        _sendEndpointProvider = sendEndpointProvider;

        _cancel = RetryUsingHostConfigurationAsync;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
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

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return _cancel(async clientContext =>
        {
            var deleted = await clientContext.DeleteScheduledMessageAsync(tokenId, cancellationToken).ConfigureAwait(false);
            if (deleted)
                LogContext.Debug?.Log("CANCEL {TokenId}", tokenId);
        }, cancellationToken);
    }

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
