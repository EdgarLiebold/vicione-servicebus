namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Internals;
using Transports;


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

    public AmazonSqsReceiveLockContext(Uri inputAddress, Message message, ReceiveSettings settings, ClientContext clientContext,
        CancellationToken cancellationToken)
        : this(inputAddress, message, settings, cancellationToken, TimeProvider.System, clientContext.ChangeMessageVisibility,
            clientContext.DeleteMessage, () => clientContext.CancellationToken.IsCancellationRequested)
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

        _visibilityTask = RenewMessageVisibility();
    }

    public async Task Complete()
    {
        try
        {
            await StopRenewal().ConfigureAwait(false);
            await _deleteMessage(_entityName, _message.ReceiptHandle, _cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _locked, 0);
            DisposeRenewalTokens();
        }
    }

    public async Task Faulted(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        await StopRenewal().ConfigureAwait(false);

        try
        {
            if (!_connectionCancellationRequested() && _queueUrl != null)
            {
                await _changeMessageVisibility(_queueUrl, _message.ReceiptHandle, _redeliverVisibilityTimeout, _cancellationToken)
                    .ConfigureAwait(false);
            }
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
        finally
        {
            Interlocked.Exchange(ref _locked, 0);
            DisposeRenewalTokens();
        }
    }

    public Task ValidateLockStatus()
    {
        if (Volatile.Read(ref _locked) == 1)
            return Task.CompletedTask;

        throw new TransportException(_inputAddress, $"Message Lock Lost: {_message.ReceiptHandle}");
    }

    async Task RenewMessageVisibility()
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

    async Task StopRenewal()
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
