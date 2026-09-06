using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Settles an Azure Service Bus session delivery through its processor callback context.</summary>
public class ServiceBusSessionMessageLockContext :
    MessageLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ServiceBusReceivedMessage _message;
    readonly ProcessSessionMessageEventArgs _session;
    bool _deadLettered;

    /// <summary>Initializes settlement for a received session message.</summary>
    /// <param name="session">The session-processor callback context used for settlement.</param>
    /// <param name="message">The received message to settle.</param>
    /// <param name="cancellationToken">The processor callback token passed to SDK settlement calls.</param>
    public ServiceBusSessionMessageLockContext(ProcessSessionMessageEventArgs session, ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        _session = session;
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
            : _session.CompleteMessageAsync(_message, _cancellationToken);
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

        return _session.AbandonMessageAsync(_message, dictionary, _cancellationToken);
    }

    /// <summary>Dead-letters the message with the transport's generic dead-letter reason.</summary>
    /// <param name="cancellationToken">The token that cancels the settlement request.</param>
    /// <returns>The dead-letter operation whose success is recorded by this settlement context.</returns>
    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string reason = "dead-letter";

        var headers = new Dictionary<string, object> { { MessageHeaders.Reason, reason } };

        await _session.DeadLetterMessageAsync(_message, headers, reason, cancellationToken: _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }

    /// <summary>Dead-letters the message with serialized exception details and a fault reason.</summary>
    /// <param name="exception">The failure serialized into dead-letter properties and description.</param>
    /// <param name="cancellationToken">The token checked before settlement begins.</param>
    /// <returns>The dead-letter operation that stores the serialized failure and records settlement in this context.</returns>
    public async Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string reason = "fault";

        (Dictionary<string, object> dictionary, var message) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        await _session.DeadLetterMessageAsync(_message, dictionary, reason, message, _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }
}
