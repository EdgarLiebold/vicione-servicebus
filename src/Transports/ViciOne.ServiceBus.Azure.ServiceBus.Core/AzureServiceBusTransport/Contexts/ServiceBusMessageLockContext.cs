using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusMessageLockContext :
    MessageLockContext
{
    readonly CancellationToken _cancellationToken;
    readonly ProcessMessageEventArgs _eventArgs;
    readonly ServiceBusReceivedMessage _message;
    bool _deadLettered;

    public ServiceBusMessageLockContext(ProcessMessageEventArgs eventArgs, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        _eventArgs = eventArgs;
        _message = message;
        _cancellationToken = cancellationToken;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _deadLettered
                    ? Task.CompletedTask
                    : _eventArgs.CompleteMessageAsync(_message, _cancellationToken);
    }

    public Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (_deadLettered)
            return Task.CompletedTask;

        (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        return _eventArgs.AbandonMessageAsync(_message, dictionary, _cancellationToken);
    }

    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _eventArgs.DeadLetterMessageAsync(_message, new Dictionary<string, object> { { MessageHeaders.Reason, "dead-letter" } }, _cancellationToken)
                    .ConfigureAwait(false);

        _deadLettered = true;
    }

    public async Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); (Dictionary<string, object> dictionary, _) = ExceptionUtil.GetExceptionHeaderDetail(exception, ServiceBusSendTransportContext.Adapter);

        await _eventArgs.DeadLetterMessageAsync(_message, dictionary, _cancellationToken).ConfigureAwait(false);

        _deadLettered = true;
    }
}
