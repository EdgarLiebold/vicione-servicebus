using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport.Middleware;

/// <summary>Polls a SQL transport queue and dispatches admitted messages to the endpoint receive pipeline.</summary>
public sealed class SqlMessageReceiver :
    ConsumerAgent<Guid>
{
    readonly ClientContext _client;
    readonly SqlReceiveEndpointContext _context;
    readonly IPartitionedTaskExecutor<SqlTransportMessage> _executorPool;
    readonly object _lock = new();
    readonly object _pendingMessagesLock = new();
    readonly SqlQueueMaintenance _maintenance;
    readonly ReceiveSettings _receiveSettings;
    readonly TimeProvider _timeProvider;
    CancellationTokenSource? _cancellationTokenSource;
    TaskCompletionSource? _pendingMessagesCompleted;
    int _pendingMessageCount;
    long _messageHandledVersion;

    /// <summary>Creates and starts a receiver for one SQL queue endpoint.</summary>
    /// <param name="client">The SQL client used for polling, settlement, and maintenance.</param>
    /// <param name="context">The receive endpoint that owns dispatch and receiver lifetime.</param>
    public SqlMessageReceiver(ClientContext client, SqlReceiveEndpointContext context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _context = context;
        _timeProvider = context.GetTimeProvider();

        _receiveSettings = client.GetPayload<ReceiveSettings>();
        _maintenance = new SqlQueueMaintenance(client, _receiveSettings, _timeProvider);

        _executorPool = new PartitionedTaskExecutor<SqlTransportMessage>(
            PartitionKeyProvider,
            _receiveSettings.ConcurrentMessageLimit,
            _receiveSettings.ConcurrentDeliveryLimit);

        TrySetConsumeTask(ConsumeAsync());
    }

    /// <summary>Waits for child agents, then drains and disposes every activated ordered-delivery partition.</summary>
    /// <param name="context">The receiver shutdown context.</param>
    /// <returns>A task that completes when receiver-owned processing has stopped.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

        Task? pendingMessages;
        lock (_pendingMessagesLock)
            pendingMessages = _pendingMessagesCompleted?.Task;

        if (pendingMessages is not null)
            await pendingMessages.WaitAsync(context.CancellationToken).ConfigureAwait(false);

        await _executorPool.DisposeAsync().ConfigureAwait(false);
    }

    async Task ConsumeAsync()
    {
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = _receiveSettings.PrefetchCount,
            ConcurrentResultLimit = _context.ConcurrentMessageLimit ?? _context.PrefetchCount,
            RequestResultLimit = _receiveSettings.PrefetchCount
        }, _timeProvider);

        SetReady();

        async Task HandleAsync(SqlTransportMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var lockContext = new SqlReceiveLockContext(_context.InputAddress, message, _receiveSettings, _client, _timeProvider);

                if (_receiveSettings.ReceiveMode == SqlReceiveMode.Normal)
                    await HandleMessageAsync(message, lockContext).ConfigureAwait(false);
                else
                    await _executorPool.ExecuteAsync(message, () => HandleMessageAsync(message, lockContext), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CompleteFetchedMessage();
            }
        }

        try
        {
            while (!IsStopping)
                await algorithm.RunAsync((messageLimit, _) => ReceiveMessagesAsync(messageLimit, Stopping), (m, c) => HandleAsync(m, c),
                    CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (IsStopping)
        {
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Consume Loop faulted");
        }
    }

    async Task HandleMessageAsync(SqlTransportMessage message, SqlReceiveLockContext lockContext)
    {
        try
        {
            if (IsStopping)
            {
                await ReleaseFetchedMessageDuringShutdownAsync(lockContext).ConfigureAwait(false);
                return;
            }

            if (message.ExpirationTime.HasValue && message.ExpirationTime.Value <= _timeProvider.GetUtcNow().UtcDateTime)
            {
                if (_receiveSettings.DeadLetterExpiredMessages)
                    await lockContext.ExpiredAsync().ConfigureAwait(false);
                else
                    await lockContext.CompleteAsync().ConfigureAwait(false);
            }
            else
            {
                var context = new SqlReceiveContext(message, _context, _receiveSettings, _client, _client.ConnectionContext, lockContext);
                try
                {
                    await DispatchAsync(message.TransportMessageId, context, lockContext).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    context.LogTransportFaulted(exception);
                }
                finally
                {
                    context.Dispose();
                }
            }
        }
        finally
        {
            MessageHandled();
        }
    }

    async Task ReleaseFetchedMessageDuringShutdownAsync(SqlReceiveLockContext lockContext)
    {
        try
        {
            await lockContext.ScheduleRedeliveryAsync(TimeSpan.Zero, null, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Debug?.Log(exception, "Could not release a fetched SQL message while the receiver was stopping");
        }
    }

    async Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(int messageLimit, CancellationToken cancellationToken)
    {
        try
        {
            long handledVersion;
            lock (_lock)
                handledVersion = _messageHandledVersion;

            IList<SqlTransportMessage> messages = (await _client.ReceiveMessagesAsync(_receiveSettings.EntityName, _receiveSettings.ReceiveMode, messageLimit,
                _receiveSettings.ConcurrentDeliveryLimit, _receiveSettings.LockDuration, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

            if (messages.Count > 0)
            {
                RegisterFetchedMessages(messages.Count);
                if (IsStopping)
                {
                    try
                    {
                        return await ReleaseFetchedMessagesDuringShutdownAsync(messages).ConfigureAwait(false);
                    }
                    finally
                    {
                        for (int i = 0; i < messages.Count; i++)
                            CompleteFetchedMessage();
                    }
                }

                return messages;
            }

            await _maintenance.RunAsync(cancellationToken, Stopping).ConfigureAwait(false);

            await WaitForPollingIntervalOrMessageHandledAsync(cancellationToken, handledVersion).ConfigureAwait(false);

            return messages;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || IsStopping)
        {
            return [];
        }
    }

    async Task<IEnumerable<SqlTransportMessage>> ReleaseFetchedMessagesDuringShutdownAsync(IEnumerable<SqlTransportMessage> messages)
    {
        foreach (SqlTransportMessage message in messages)
        {
            var lockContext = new SqlReceiveLockContext(_context.InputAddress, message, _receiveSettings, _client, _timeProvider);
            await ReleaseFetchedMessageDuringShutdownAsync(lockContext).ConfigureAwait(false);
        }

        return [];
    }

    async Task WaitForPollingIntervalOrMessageHandledAsync(CancellationToken cancellationToken, long handledVersion)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        lock (_lock)
        {
            if (_messageHandledVersion != handledVersion)
            {
                cancellationTokenSource.Dispose();
                return;
            }

            _cancellationTokenSource = cancellationTokenSource;
        }

        try
        {
            var delayTask = _receiveSettings.QueueId.HasValue
                ? _client.ConnectionContext.DelayUntilMessageReadyAsync(_receiveSettings.QueueId.Value, _maintenance.MaximumPollingInterval,
                    _timeProvider, cancellationTokenSource.Token)
                : Task.Delay(_maintenance.MaximumPollingInterval, _timeProvider, cancellationTokenSource.Token);

            await delayTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_lock)
            {
                if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
                    _cancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    /// <summary>Reports that the message has been handled.</summary>
    public void MessageHandled()
    {
        lock (_lock)
        {
            _messageHandledVersion++;
            _cancellationTokenSource?.Cancel();
        }
    }

    void RegisterFetchedMessages(int count)
    {
        lock (_pendingMessagesLock)
        {
            if (_pendingMessageCount == 0)
                _pendingMessagesCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            _pendingMessageCount += count;
        }
    }

    void CompleteFetchedMessage()
    {
        lock (_pendingMessagesLock)
        {
            if (--_pendingMessageCount == 0)
                _pendingMessagesCompleted?.TrySetResult();
        }
    }

    static byte[] PartitionKeyProvider(SqlTransportMessage message)
    {
        return string.IsNullOrEmpty(message.PartitionKey)
            ? []
            : Encoding.UTF8.GetBytes(message.PartitionKey);
    }
}
