using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for no lock receive operations.</summary>
public class NoLockReceiveContext :
    ReceiveLockContext
{
    /// <summary>Exposes the instance used by the containing type.</summary>
    public static readonly ReceiveLockContext Instance = new NoLockReceiveContext();

    NoLockReceiveContext()
    {
    }

    /// <summary>Marks the current operation as complete.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        _ = exception;
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    /// <summary>Validates lock status.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
