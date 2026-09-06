using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    readonly OrderedPartitionedTaskExecutor _executorPool;
    readonly object _lock = new();
    readonly ReceiveSettings _receiveSettings;
    readonly TimeProvider _timeProvider;
    readonly TimeSpan? _touchQueueInterval;
    CancellationTokenSource? _cancellationTokenSource;
    DateTime? _lastMaintenance;
    DateTime? _lastTouched;

    /// <summary>Fetches messages from the SQL transport and dispatches them to the receive pipeline.</summary>
    /// <param name="client">The model context for the consumer.</param>
    /// <param name="context">The topology.</param>
    public SqlMessageReceiver(ClientContext client, SqlReceiveEndpointContext context)
        : base(context)
    {
        _client = client;
        _context = context;
        _timeProvider = context.GetTimeProvider();

        _receiveSettings = client.GetPayload<ReceiveSettings>();

        if (_receiveSettings.AutoDeleteOnIdle.HasValue)
            _touchQueueInterval = new TimeSpan(_receiveSettings.AutoDeleteOnIdle.Value.Ticks / 2);

        _executorPool = new OrderedPartitionedTaskExecutor(_receiveSettings);

        TrySetConsumeTask(ConsumeAsync());
    }

    /// <summary>Reports that active and actual agents has completed.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        await base.ActiveAndActualAgentsCompletedAsync(context).ConfigureAwait(false);

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

        Task HandleAsync(SqlTransportMessage message, CancellationToken cancellationToken)
        {
            var lockContext = new SqlReceiveLockContext(_context.InputAddress, message, _receiveSettings, _client, _timeProvider);

            return _receiveSettings.ReceiveMode == SqlReceiveMode.Normal
                ? HandleMessageAsync(message, lockContext)
                : _executorPool.ExecuteAsync(message, () => HandleMessageAsync(message, lockContext), cancellationToken);
        }

        try
        {
            while (!IsStopping)
                await algorithm.RunAsync((messageLimit, token) => ReceiveMessagesAsync(messageLimit, token), (m, c) => HandleAsync(m, c), Stopping).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == Stopping)
        {
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Consume Loop faulted");
        }
    }

    async Task HandleMessageAsync(SqlTransportMessage message, SqlReceiveLockContext lockContext)
    {
        if (IsStopping)
            return;

        if (message.ExpirationTime.HasValue && message.ExpirationTime.Value < _timeProvider.GetUtcNow().UtcDateTime)
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

            MessageHandled();
        }
    }

    async Task<IEnumerable<SqlTransportMessage>> ReceiveMessagesAsync(int messageLimit, CancellationToken cancellationToken)
    {
        try
        {
            IList<SqlTransportMessage> messages = (await _client.ReceiveMessagesAsync(_receiveSettings.EntityName, _receiveSettings.ReceiveMode, messageLimit,
                _receiveSettings.ConcurrentDeliveryLimit, _receiveSettings.LockDuration, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

            if (messages.Count > 0)
                return messages;

            try
            {
                int? count = 0;

                var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

                if (_lastMaintenance.HasValue == false || _lastMaintenance.Value + TimeSpan.FromSeconds(30) < utcNow)
                {
                    count = await _client.DeadLetterQueueAsync(_receiveSettings.QueueName, _receiveSettings.MaintenanceBatchSize, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (count < _receiveSettings.MaintenanceBatchSize)
                        _lastMaintenance = utcNow;
                }

                if (_touchQueueInterval.HasValue && count is null or 0)
                {
                    if (_lastTouched.HasValue == false || _lastTouched.Value + _touchQueueInterval.Value < utcNow)
                    {
                        await _client.TouchQueueAsync(_receiveSettings.EntityName, cancellationToken: cancellationToken).ConfigureAwait(false);

                        _lastTouched = utcNow;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
            }


            await WaitForPollingIntervalOrMessageHandledAsync(cancellationToken).ConfigureAwait(false);

            return messages;
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    async Task WaitForPollingIntervalOrMessageHandledAsync(CancellationToken cancellationToken)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        lock (_lock)
            _cancellationTokenSource = cancellationTokenSource;

        try
        {
            var delayTask = _receiveSettings.QueueId.HasValue
                ? _client.ConnectionContext.DelayUntilMessageReadyAsync(_receiveSettings.QueueId.Value, _receiveSettings.PollingInterval,
                    _timeProvider, cancellationTokenSource.Token)
                : Task.Delay(_receiveSettings.PollingInterval, _timeProvider, cancellationTokenSource.Token);

            await delayTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
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
            _cancellationTokenSource?.Cancel();
    }


    class OrderedPartitionedTaskExecutor :
        IPartitionedTaskExecutor<SqlTransportMessage>
    {
        readonly IPartitionedTaskExecutor<SqlTransportMessage> _keyExecutorPool;

        public OrderedPartitionedTaskExecutor(ReceiveSettings receiveSettings)
        {
            IHashGenerator hashGenerator = new Murmur3UnsafeHashGenerator();
            _keyExecutorPool = new PartitionedTaskExecutor<SqlTransportMessage>(PartitionKeyProvider, hashGenerator,
                receiveSettings.ConcurrentMessageLimit, receiveSettings.ConcurrentDeliveryLimit);
        }

        public Task EnqueueAsync(SqlTransportMessage result, Func<Task> handle, CancellationToken cancellationToken)
        {
            return _keyExecutorPool.EnqueueAsync(result, handle, cancellationToken);
        }

        public Task ExecuteAsync(SqlTransportMessage result, Func<Task> method, CancellationToken cancellationToken = default)
        {
            return _keyExecutorPool.ExecuteAsync(result, method, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _keyExecutorPool.DisposeAsync();
        }

        static byte[] PartitionKeyProvider(SqlTransportMessage message)
        {
            return string.IsNullOrEmpty(message.PartitionKey)
                ? []
                : Encoding.UTF8.GetBytes(message.PartitionKey);
        }
    }
}
