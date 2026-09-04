using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqReceiveLockContext :
    ReceiveLockContext
{
    readonly IMessage _message;

    public ActiveMqReceiveLockContext(IMessage message)
    {
        _message = message;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _message.AcknowledgeAsync();
    }

    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
