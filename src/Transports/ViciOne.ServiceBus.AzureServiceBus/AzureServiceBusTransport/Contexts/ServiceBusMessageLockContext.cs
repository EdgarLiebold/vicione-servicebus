using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus message lock context implementation.
/// </summary>
public class ServiceBusMessageLockContext :
    MessageLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ProcessMessageEventArgs _eventArgs;
    readonly ServiceBusReceivedMessage _message;
    bool _deadLettered;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ServiceBusMessageLockContext(ProcessMessageEventArgs eventArgs, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        _eventArgs = eventArgs;
        _message = message;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _deadLettered
                    ? Task.CompletedTask
                    : _eventArgs.CompleteMessageAsync(_message, _cancellationToken);
    }

    /// <summary>
    /// Performs the abandon operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (_deadLettered)
            return Task.CompletedTask;

        (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        return _eventArgs.AbandonMessageAsync(_message, dictionary, _cancellationToken);
    }

    /// <summary>
    /// Performs the dead letter operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _eventArgs.DeadLetterMessageAsync(_message, new Dictionary<string, object> { { MessageHeaders.Reason, "dead-letter" } }, _cancellationToken)
                    .ConfigureAwait(false);

        _deadLettered = true;
    }

    /// <summary>
    /// Performs the dead letter operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        await _eventArgs.DeadLetterMessageAsync(_message, dictionary, _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }
}
