using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class EventHubReceiveLockContext :
    ReceiveLockContext
{
    readonly ProcessEventArgs _eventArgs;
    readonly IProcessorLockContext _lockContext;

    public EventHubReceiveLockContext(ProcessEventArgs eventArgs, IProcessorLockContext lockContext)
    {
        _eventArgs = eventArgs;
        _lockContext = lockContext;
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return _lockContext.CompleteAsync(_eventArgs, cancellationToken: cancellationToken);
    }

    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return _lockContext.FaultedAsync(_eventArgs, exception, cancellationToken: cancellationToken);
    }

    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
