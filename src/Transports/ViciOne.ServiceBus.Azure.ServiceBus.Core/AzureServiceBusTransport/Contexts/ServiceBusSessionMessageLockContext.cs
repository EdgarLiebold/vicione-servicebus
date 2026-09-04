using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusSessionMessageLockContext :
    MessageLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ServiceBusReceivedMessage _message;
    readonly ProcessSessionMessageEventArgs _session;
    bool _deadLettered;

    public ServiceBusSessionMessageLockContext(ProcessSessionMessageEventArgs session, ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        _session = session;
        _message = message;
        _cancellationToken = cancellationToken;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _deadLettered
                    ? Task.CompletedTask
                    : _session.CompleteMessageAsync(_message, _cancellationToken);
    }

    public Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (_deadLettered)
            return Task.CompletedTask;

        (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        return _session.AbandonMessageAsync(_message, dictionary, _cancellationToken);
    }

    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        const string reason = "dead-letter";

        var headers = new Dictionary<string, object> { { MessageHeaders.Reason, reason } };

        await _session.DeadLetterMessageAsync(_message, headers, reason, cancellationToken: _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }

    public async Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); const string reason = "fault";

        (Dictionary<string, object> dictionary, var message) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        await _session.DeadLetterMessageAsync(_message, dictionary, reason, message, _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }
}
