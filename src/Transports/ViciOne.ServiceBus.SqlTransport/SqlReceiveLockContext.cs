using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Owns renewal and terminal state transitions for one locked SQL transport delivery.</summary>
public class SqlReceiveLockContext :
    MessageRedeliveryContext,
    ReceiveLockContext
{
    readonly CancellationTokenSource _activeTokenSource;
    readonly ClientContext _clientContext;
    readonly Uri _inputAddress;
    readonly SqlTransportMessage _message;
    readonly Task? _renewLockTask;
    readonly SemaphoreSlim _settlementGate;
    readonly ReceiveSettings _settings;
    readonly DateTime _startedAt;
    readonly TimeProvider _timeProvider;
    int _locked;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="clientContext">The client context.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public SqlReceiveLockContext(Uri inputAddress, SqlTransportMessage message, ReceiveSettings settings, ClientContext clientContext,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(inputAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(clientContext);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
        _startedAt = timeProvider.GetUtcNow().UtcDateTime;
        _inputAddress = inputAddress;
        _message = message;
        _settings = settings;
        _clientContext = clientContext;
        _activeTokenSource = new CancellationTokenSource();
        _settlementGate = new SemaphoreSlim(1, 1);
        _locked = 1;

        if (_message.LockId.HasValue)
            _renewLockTask = RenewLockAsync();
    }

    /// <summary>Releases the delivery lock and defers the next delivery attempt.</summary>
    /// <param name="delay">The delay before the delivery becomes eligible again.</param>
    /// <param name="callback">An optional send-context callback, which native SQL lock release cannot represent.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback, CancellationToken cancellationToken = default)
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The redelivery delay cannot be negative.");
        if (callback != null)
        {
            throw new NotSupportedException(
                "The SQL transport reschedules a delivery by releasing its database lock and cannot apply a send-context callback.");
        }
        return SettleAsync(async () =>
        {
            _clientContext.CancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();

            if (!_message.LockId.HasValue)
                throw LockLost("reschedule");

            var transportHeaders = _message.GetTransportHeaders();
            var redeliveryCount = transportHeaders.Get(MessageHeaders.RedeliveryCount, default(int?)) ?? 0;
            transportHeaders.Set(MessageHeaders.RedeliveryCount, redeliveryCount + 1);

            bool unlocked = await _clientContext.UnlockAsync(
                _message.LockId.Value,
                _message.MessageDeliveryId,
                delay,
                transportHeaders,
                cancellationToken).ConfigureAwait(false);

            if (!unlocked)
                throw LockLost("reschedule");

            LogContext.Debug?.Log("RESEND {DestinationAddress} {MessageId} (delay: {Delay})", _inputAddress, _message.MessageId, delay);
        }, cancellationToken);
    }

    /// <summary>Marks the current operation as complete.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return SettleAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_message.LockId.HasValue)
                throw LockLost("complete");

            bool deleted = await _clientContext.DeleteMessageAsync(
                _message.LockId.Value,
                _message.MessageDeliveryId,
                cancellationToken).ConfigureAwait(false);

            if (!deleted)
                throw LockLost("complete");
        }, cancellationToken);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return SettleAsync(async () =>
        {
            _clientContext.CancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_message.LockId.HasValue)
                throw LockLost("release after a fault");

            var headers = _message.GetTransportHeaders();
            try
            {
                exception = exception.GetBaseException() ?? exception;
            }
            catch
            {
                // Preserve the original fault so the locked message can still be released.
            }

            var exceptionMessage = ExceptionUtil.GetMessage(exception);

            headers.Set(MessageHeaders.Reason, "fault");
            headers.Set(MessageHeaders.FaultExceptionType, TypeCache.GetShortName(exception.GetType()));
            headers.Set(MessageHeaders.FaultMessage, exceptionMessage);
            headers.Set(MessageHeaders.FaultStackTrace, ExceptionUtil.GetStackTrace(exception));

            bool unlocked = await _clientContext.UnlockAsync(
                _message.LockId.Value,
                _message.MessageDeliveryId,
                _settings.UnlockDelay ?? TimeSpan.Zero,
                headers,
                cancellationToken).ConfigureAwait(false);

            if (!unlocked)
                throw LockLost("release after a fault");
        }, cancellationToken);
    }

    /// <summary>Validates lock status.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Volatile.Read(ref _locked) == 1)
            return Task.CompletedTask;

        throw LockLost("validate");
    }

    /// <summary>Creates an expired result.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExpiredAsync(CancellationToken cancellationToken = default)
    {
        var transportHeaders = SqlTransportMessage.DeserializeHeaders(_message.TransportHeaders);
        transportHeaders.Set(MessageHeaders.Reason, "expired");

        await MoveToQueueAsync(
            _settings.QueueName,
            SqlQueueType.DeadLetterQueue,
            _message.ExpirationTime,
            transportHeaders,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Moves the locked delivery to an auxiliary queue and records that no further settlement is required.</summary>
    /// <param name="queueName">The logical queue whose auxiliary destination receives the delivery.</param>
    /// <param name="queueType">The auxiliary destination kind.</param>
    /// <param name="expirationTime">The expiration time to persist on the moved delivery.</param>
    /// <param name="transportHeaders">The transport headers to persist on the moved delivery.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal async Task MoveToQueueAsync(
        string queueName,
        SqlQueueType queueType,
        DateTimeOffset? expirationTime,
        SendHeaders transportHeaders,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(transportHeaders);

        await SettleAsync(async () =>
        {
            _clientContext.CancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_message.LockId.HasValue)
                throw LockLost("move");

            bool moved = await _clientContext.MoveMessageAsync(
                _message.LockId.Value,
                _message.MessageDeliveryId,
                queueName,
                queueType,
                expirationTime,
                transportHeaders,
                cancellationToken).ConfigureAwait(false);

            if (!moved)
                throw LockLost("move");
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task RenewLockAsync()
    {
        TimeSpan CalculateDelay(TimeSpan timeout)
        {
            return TimeSpan.FromSeconds(timeout.TotalSeconds * 0.7);
        }

        var duration = _settings.LockDuration;

        var delay = CalculateDelay(duration);

        duration = TimeSpan.FromSeconds(Math.Min(60, duration.TotalSeconds));

        while (!_activeTokenSource.IsCancellationRequested)
        {
            try
            {
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, _timeProvider, _activeTokenSource.Token).ConfigureAwait(false);
                }

                if (_activeTokenSource.IsCancellationRequested)
                    break;

                if (_message.LockId.HasValue)
                {
                    if (!await _clientContext.RenewLockAsync(
                            _message.LockId.Value,
                            _message.MessageDeliveryId,
                            duration,
                            _activeTokenSource.Token).ConfigureAwait(false))
                    {
                        LogContext.Warning?.Log("Message Lock Lost: {InputAddress} - {MessageDeliveryId} ({LockId})", _inputAddress,
                            _message.MessageDeliveryId, _message.LockId);

                        Volatile.Write(ref _locked, 0);

                        break;
                    }
                }

                if (_timeProvider.GetUtcNow().UtcDateTime - _startedAt + duration >= _settings.MaxLockDuration)
                    break;

                delay = CalculateDelay(duration);
            }
            catch (TimeoutException)
            {
                delay = TimeSpan.Zero;
            }
            catch (OperationCanceledException) when (_activeTokenSource.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Volatile.Write(ref _locked, 0);
                LogContext.Warning?.Log(exception, "Message lock renewal failed: {InputAddress} {MessageDeliveryId} {LockId}", _inputAddress,
                    _message.MessageDeliveryId, _message.LockId);
                break;
            }
        }
    }

    async Task SettleAsync(Func<Task> settle, CancellationToken cancellationToken)
    {
        await _settlementGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (Interlocked.CompareExchange(ref _locked, 0, 1) != 1)
                return;

            _activeTokenSource.Cancel();

            try
            {
                if (_renewLockTask != null)
                    await _renewLockTask.ConfigureAwait(false);

                await settle().ConfigureAwait(false);
            }
            finally
            {
                _activeTokenSource.Dispose();
            }
        }
        finally
        {
            _settlementGate.Release();
        }
    }

    TransportException LockLost(string operation)
    {
        return new TransportException(
            _inputAddress,
            $"The SQL delivery lock was lost while attempting to {operation} message delivery {_message.MessageDeliveryId} ({_message.LockId}).");
    }
}
