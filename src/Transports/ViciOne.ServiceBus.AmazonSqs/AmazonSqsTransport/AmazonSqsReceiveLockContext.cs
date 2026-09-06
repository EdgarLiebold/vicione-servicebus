using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Renews and settles the Amazon SQS visibility lock for a received message.</summary>
public class AmazonSqsReceiveLockContext :
    ReceiveLockContext
{
    readonly CancellationTokenSource _activeTokenSource;
    readonly CancellationToken _cancellationToken;
    readonly Func<string, string, int, CancellationToken, Task> _changeMessageVisibility;
    readonly Func<bool> _connectionCancellationRequested;
    readonly Func<string, string, CancellationToken, Task> _deleteMessage;
    readonly string _entityName;
    readonly Uri _inputAddress;
    readonly TimeSpan _maxVisibilityTimeout;
    readonly int _maxVisibilityTimeoutRenewal;
    readonly Message _message;
    readonly string? _queueUrl;
    readonly int _redeliverVisibilityTimeout;
    readonly CancellationTokenSource _renewalTokenSource;
    readonly long _startedAt;
    readonly TimeProvider _timeProvider;
    readonly int _visibilityTimeout;
    readonly Task _visibilityTask;
    int _locked;

    /// <summary>Starts visibility-timeout renewal for a received Amazon SQS message.</summary>
    /// <param name="inputAddress">The queue endpoint address used in lock-loss errors.</param>
    /// <param name="message">The received Amazon SQS message.</param>
    /// <param name="settings">The queue's visibility and redelivery settings.</param>
    /// <param name="clientContext">The client context used to change visibility and delete the message.</param>
    /// <param name="cancellationToken">The token that stops visibility renewal.</param>
    public AmazonSqsReceiveLockContext(Uri inputAddress, Message message, ReceiveSettings settings, ClientContext clientContext,
        CancellationToken cancellationToken)
        : this(inputAddress, message, settings, cancellationToken, TimeProvider.System, clientContext.ChangeMessageVisibilityAsync,
            clientContext.DeleteMessageAsync, () => clientContext.CancellationToken.IsCancellationRequested)
    {
    }

    internal AmazonSqsReceiveLockContext(Uri inputAddress, Message message, ReceiveSettings settings, CancellationToken cancellationToken,
        TimeProvider timeProvider, Func<string, string, int, CancellationToken, Task> changeMessageVisibility,
        Func<string, string, CancellationToken, Task> deleteMessage, Func<bool> connectionCancellationRequested)
    {
        ArgumentNullException.ThrowIfNull(inputAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(changeMessageVisibility);
        ArgumentNullException.ThrowIfNull(deleteMessage);
        ArgumentNullException.ThrowIfNull(connectionCancellationRequested);

        _inputAddress = inputAddress;
        _message = message;
        _cancellationToken = cancellationToken;
        _timeProvider = timeProvider;
        _changeMessageVisibility = changeMessageVisibility;
        _deleteMessage = deleteMessage;
        _connectionCancellationRequested = connectionCancellationRequested;

        // A receive lock is a runtime snapshot. Later endpoint configuration changes must not
        // alter the timing or settlement contract of a message that is already in flight.
        _entityName = settings.EntityName;
        _queueUrl = settings.QueueUrl;
        _visibilityTimeout = settings.VisibilityTimeout;
        _maxVisibilityTimeout = settings.MaxVisibilityTimeout;
        _maxVisibilityTimeoutRenewal = settings.MaxVisibilityTimeoutRenewal;
        _redeliverVisibilityTimeout = settings.RedeliverVisibilityTimeout;

        _startedAt = _timeProvider.GetTimestamp();
        _activeTokenSource = new CancellationTokenSource();
        _renewalTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_activeTokenSource.Token, cancellationToken);
        _locked = 1;

        _visibilityTask = RenewMessageVisibilityAsync();
    }

    /// <summary>Stops visibility renewal and deletes the message from Amazon SQS.</summary>
    /// <param name="cancellationToken">The caller token used to cancel provider deletion after renewal has stopped.</param>
    /// <returns>A task that completes when renewal has stopped and the message has been deleted.</returns>
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var settlementLifetime = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

        try
        {
            await StopRenewalAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await _deleteMessage(_entityName, _message.ReceiptHandle, settlementLifetime.Token).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _locked, 0);
            DisposeRenewalTokens();
        }
    }

    /// <summary>Stops visibility renewal and makes the message eligible for redelivery after the configured delay.</summary>
    /// <param name="exception">The consumer exception retained for diagnostic logging if redelivery preparation also fails.</param>
    /// <param name="cancellationToken">The caller token used to cancel provider settlement after renewal has stopped.</param>
    /// <returns>A task that completes after renewal and redelivery preparation have stopped.</returns>
    public async Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(exception);

        using var settlementLifetime = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

        try
        {
            await StopRenewalAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!_connectionCancellationRequested() && _queueUrl != null)
                {
                    await _changeMessageVisibility(_queueUrl, _message.ReceiptHandle, _redeliverVisibilityTimeout, settlementLifetime.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested || _connectionCancellationRequested())
            {
            }
            catch (MessageNotInflightException)
            {
            }
            catch (ReceiptHandleIsInvalidException)
            {
            }
            catch (Exception redeliveryException)
            {
                LogContext.Error?.Log(redeliveryException, "ChangeMessageVisibility failed: {ReceiptHandle}, Original Exception: {Exception}",
                    _message.ReceiptHandle, exception);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _locked, 0);
            DisposeRenewalTokens();
        }
    }

    /// <summary>Verifies that the message visibility lock is still active.</summary>
    /// <param name="cancellationToken">The token used to cancel validation.</param>
    /// <returns>A completed task while the lock remains active.</returns>
    /// <exception cref="TransportException">The visibility lock has been lost or settlement has completed.</exception>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (Volatile.Read(ref _locked) == 1)
            return Task.CompletedTask;

        throw new TransportException(_inputAddress, $"Message Lock Lost: {_message.ReceiptHandle}");
    }

    async Task RenewMessageVisibilityAsync()
    {
        var delay = CalculateDelay(_visibilityTimeout);

        while (Volatile.Read(ref _locked) == 1 && !_renewalTokenSource.IsCancellationRequested)
        {
            try
            {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, _timeProvider, _renewalTokenSource.Token).ConfigureAwait(false);

                if (_renewalTokenSource.IsCancellationRequested)
                    return;

                var elapsed = _timeProvider.GetElapsedTime(_startedAt);
                var remaining = _maxVisibilityTimeout - elapsed;
                var renewalSeconds = Math.Min(_maxVisibilityTimeoutRenewal, (int)Math.Floor(remaining.TotalSeconds));
                if (renewalSeconds <= 0)
                {
                    LogContext.Warning?.Log("Maximum visibility timeout {MaxVisibilityTimeout} for message {ReceiptHandle} reached.",
                        _maxVisibilityTimeout, _message.ReceiptHandle);
                    Interlocked.Exchange(ref _locked, 0);
                    return;
                }

                if (_queueUrl != null)
                    await _changeMessageVisibility(_queueUrl, _message.ReceiptHandle, renewalSeconds, _renewalTokenSource.Token)
                        .ConfigureAwait(false);

                delay = CalculateDelay(renewalSeconds);
            }
            catch (MessageNotInflightException exception)
            {
                LogContext.Warning?.Log(exception, "Message no longer in flight: {ReceiptHandle}", _message.ReceiptHandle);
                Interlocked.Exchange(ref _locked, 0);
                return;
            }
            catch (ReceiptHandleIsInvalidException exception)
            {
                LogContext.Warning?.Log(exception, "Message receipt handle is invalid: {ReceiptHandle}", _message.ReceiptHandle);
                Interlocked.Exchange(ref _locked, 0);
                return;
            }
            catch (OperationCanceledException) when (_renewalTokenSource.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogContext.Error?.Log(exception, "Failed to extend message {ReceiptHandle} visibility ({ElapsedTime})",
                    _message.ReceiptHandle, _timeProvider.GetElapsedTime(_startedAt));
                Interlocked.Exchange(ref _locked, 0);
                return;
            }
        }
    }

    async Task StopRenewalAsync()
    {
        if (!_activeTokenSource.IsCancellationRequested)
            await _activeTokenSource.CancelAsync().ConfigureAwait(false);

        await _visibilityTask.ConfigureAwait(false);
    }

    void DisposeRenewalTokens()
    {
        _renewalTokenSource.Dispose();
        _activeTokenSource.Dispose();
    }

    static TimeSpan CalculateDelay(int visibilityTimeout) =>
        visibilityTimeout <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(visibilityTimeout * 0.7);
}
