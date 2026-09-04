using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs receive lock context implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="clientContext">The client context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); try
        {
            await StopRenewalAsync().ConfigureAwait(false);
            await _deleteMessage(_entityName, _message.ReceiptHandle, _cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _locked, 0);
            DisposeRenewalTokens();
        }
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(exception);

        await StopRenewalAsync().ConfigureAwait(false);

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

    /// <summary>
    /// Validates lock status.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (Volatile.Read(ref _locked) == 1)
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
