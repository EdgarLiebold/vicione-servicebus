using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusReceiveLockContext :
    ReceiveLockContext
{
    readonly Uri _inputAddress;
    readonly MessageLockContext _lockContext;
    readonly ServiceBusReceivedMessage _message;
    readonly TimeProvider _timeProvider;

    public ServiceBusReceiveLockContext(Uri inputAddress, MessageLockContext lockContext, ServiceBusReceivedMessage message, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _inputAddress = inputAddress;
        _lockContext = lockContext;
        _message = message;
        _timeProvider = timeProvider;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return _lockContext.CompleteAsync(cancellationToken: cancellationToken);
    }

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
