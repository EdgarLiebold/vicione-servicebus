using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.Context;

public class NoLockReceiveContext :
    ReceiveLockContext
{
    public static readonly ReceiveLockContext Instance = new NoLockReceiveContext();

    NoLockReceiveContext()
    {
    }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        _ = exception;
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
