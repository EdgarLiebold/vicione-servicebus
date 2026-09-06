using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Settles a non-session Azure Service Bus delivery through its processor callback context.</summary>
public class ServiceBusMessageLockContext :
    MessageLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ProcessMessageEventArgs _eventArgs;
    readonly ServiceBusReceivedMessage _message;
    bool _deadLettered;

    /// <summary>Initializes settlement for a received message.</summary>
    /// <param name="eventArgs">The processor callback context used for settlement.</param>
    /// <param name="message">The received message to settle.</param>
    /// <param name="cancellationToken">The processor callback token passed to SDK settlement calls.</param>
    public ServiceBusMessageLockContext(ProcessMessageEventArgs eventArgs, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        _eventArgs = eventArgs;
        _message = message;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Completes the message unless it has already been dead-lettered by this context.</summary>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>The Azure SDK completion task, or a completed task when this context already dead-lettered the message.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return _deadLettered
            ? Task.CompletedTask
            : _eventArgs.CompleteMessageAsync(_message, _cancellationToken);
    }

    /// <summary>Abandons the message with exception details unless it has already been dead-lettered.</summary>
    /// <param name="exception">The failure serialized into message properties.</param>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>The Azure SDK abandon task, or a completed task when this context already dead-lettered the message.</returns>
    public Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        if (_deadLettered)
            return Task.CompletedTask;

        (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        return _eventArgs.AbandonMessageAsync(_message, dictionary, _cancellationToken);
    }

    /// <summary>Dead-letters the message with the transport's generic dead-letter reason.</summary>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>The dead-letter operation whose success is recorded by this settlement context.</returns>
    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _eventArgs.DeadLetterMessageAsync(
                _message,
                new Dictionary<string, object> { { MessageHeaders.Reason, "dead-letter" } },
                _cancellationToken)
            .ConfigureAwait(false);

        _deadLettered = true;
    }

    /// <summary>Dead-letters the message with serialized exception details.</summary>
    /// <param name="exception">The failure serialized into dead-letter properties.</param>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>The dead-letter operation that stores the serialized failure and records settlement in this context.</returns>
    public async Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        await _eventArgs.DeadLetterMessageAsync(_message, dictionary, _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }
}
