using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus session message lock context implementation.
/// </summary>
public class ServiceBusSessionMessageLockContext :
    MessageLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ServiceBusReceivedMessage _message;
    readonly ProcessSessionMessageEventArgs _session;
    bool _deadLettered;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="session">The session value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ServiceBusSessionMessageLockContext(ProcessSessionMessageEventArgs session, ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        _session = session;
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
                    : _session.CompleteMessageAsync(_message, _cancellationToken);
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

        return _session.AbandonMessageAsync(_message, dictionary, _cancellationToken);
    }

    /// <summary>
    /// Performs the dead letter operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        const string reason = "dead-letter";

        var headers = new Dictionary<string, object> { { MessageHeaders.Reason, reason } };

        await _session.DeadLetterMessageAsync(_message, headers, reason, cancellationToken: _cancellationToken).ConfigureAwait(false);

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
        cancellationToken.ThrowIfCancellationRequested(); const string reason = "fault";

        (Dictionary<string, object> dictionary, var message) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        await _session.DeadLetterMessageAsync(_message, dictionary, reason, message, _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }
}
