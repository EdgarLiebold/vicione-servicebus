using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Validates and settles an Azure Service Bus delivery lock for receive-pipeline dispatch.</summary>
public class ServiceBusReceiveLockContext :
    ReceiveLockContext
{
    readonly Uri _inputAddress;
    readonly MessageLockContext _lockContext;
    readonly ServiceBusReceivedMessage _message;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates lock handling for a received message.</summary>
    /// <param name="inputAddress">The receive endpoint address used in expiration errors.</param>
    /// <param name="lockContext">The provider settlement context.</param>
    /// <param name="message">The received Azure Service Bus message.</param>
    /// <param name="timeProvider">The time source used for expiry checks.</param>
    public ServiceBusReceiveLockContext(Uri inputAddress, MessageLockContext lockContext, ServiceBusReceivedMessage message, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _inputAddress = inputAddress;
        _lockContext = lockContext;
        _message = message;
        _timeProvider = timeProvider;
    }

    /// <summary>Completes the message through the provider settlement context.</summary>
    /// <param name="cancellationToken">Cancels broker settlement.</param>
    /// <returns>A task that completes when the broker accepts settlement.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return _lockContext.CompleteAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Abandons the message after a processing failure unless the lock or connection is already unusable.</summary>
    /// <param name="exception">The processing or provider failure.</param>
    /// <param name="cancellationToken">Cancels broker settlement.</param>
    /// <returns>A task that completes after abandonment or after an ignored terminal lock failure.</returns>
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

    /// <summary>Rejects a delivery whose lock or message time to live has expired.</summary>
    /// <param name="cancellationToken">Returns a canceled task when cancellation is already requested.</param>
    /// <returns>A completed task while the lock and message remain valid.</returns>
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
