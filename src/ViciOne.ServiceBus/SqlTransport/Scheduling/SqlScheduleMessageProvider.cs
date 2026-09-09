using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Schedules and cancels messages through the SQL transport's enqueue-time support.</summary>
public class SqlScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly Func<Func<ClientContext, Task>, CancellationToken, Task> _cancel;
    readonly ConsumeContext? _context;
    readonly ISqlHostConfiguration? _hostConfiguration;
    readonly ISendEndpointProvider _sendEndpointProvider;

    /// <summary>Creates a provider that uses the SQL client attached to a consume context.</summary>
    /// <param name="context">The active SQL transport consume context.</param>
    public SqlScheduleMessageProvider(ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _sendEndpointProvider = context;

        _cancel = RetryUsingContextAsync;
    }

    /// <summary>Creates a provider that resolves SQL clients from the host connection supervisor.</summary>
    /// <param name="hostConfiguration">The SQL transport host configuration.</param>
    /// <param name="sendEndpointProvider">The provider used to resolve scheduled-message destinations.</param>
    public SqlScheduleMessageProvider(ISqlHostConfiguration hostConfiguration, ISendEndpointProvider sendEndpointProvider)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));

        _cancel = RetryUsingHostConfigurationAsync;
    }

    /// <summary>Enqueues a message for delivery at the requested time.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destinationAddress">The destination that receives the message.</param>
    /// <param name="dueAt">The earliest delivery time.</param>
    /// <param name="message">The message to schedule.</param>
    /// <param name="pipe">Additional send-context configuration.</param>
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

        return new ScheduledMessageHandle<T>(tokenId, dueAt, destinationAddress, message);
    }

    /// <summary>Cancels the scheduled message identified by its transport token.</summary>
    /// <param name="tokenId">The scheduling token assigned when the message was enqueued.</param>
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

    /// <summary>Cancels the scheduled message identified by its transport token.</summary>
    /// <param name="destinationAddress">The original destination, used for cancellation diagnostics.</param>
    /// <param name="tokenId">The scheduling token assigned when the message was enqueued.</param>
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
            context.CreateScope("sql-schedule-message-client");
        }
    }
}
