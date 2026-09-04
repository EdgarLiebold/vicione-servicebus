using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus receive lock context implementation.
/// </summary>
public class ServiceBusReceiveLockContext :
    ReceiveLockContext
{
    readonly Uri _inputAddress;
    readonly MessageLockContext _lockContext;
    readonly ServiceBusReceivedMessage _message;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="lockContext">The lock context value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ServiceBusReceiveLockContext(Uri inputAddress, MessageLockContext lockContext, ServiceBusReceivedMessage message, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _inputAddress = inputAddress;
        _lockContext = lockContext;
        _message = message;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return _lockContext.CompleteAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        switch (exception)
        {
            case MessageLockExpiredException _:
            case MessageTimeToLiveExpiredException _:
            case ServiceBusException { Reason: ServiceBusFailureReason.MessageLockLost }:
            case ServiceBusException { Reason: ServiceBusFailureReason.SessionLockLost }:
            case ServiceBusException { Reason: ServiceBusFailureReason.ServiceCommunicationProblem }:
                return;

            default:
                try
                {
                    await _lockContext.AbandonAsync(exception, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogContext.Warning?.Log(exception, "Abandon message faulted: {MessageId} - {Exception}", _message.MessageId, ex);
                }

                break;
        }
    }

    /// <summary>
    /// Validates lock status.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (_message.LockedUntil <= utcNow)
            throw new MessageLockExpiredException(_inputAddress, $"The message lock expired: {_message.MessageId}");

        if (_message.ExpiresAt < utcNow)
            throw new MessageTimeToLiveExpiredException(_inputAddress, $"The message expired: {_message.MessageId}");

        return Task.CompletedTask;
    }
}
