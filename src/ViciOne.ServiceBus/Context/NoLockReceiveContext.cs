using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Context;

/// <summary>Provides no-op settlement for transports that do not hold receive locks.</summary>
public sealed class NoLockReceiveContext :
    ReceiveLockContext
{
    /// <summary>Gets the shared stateless receive-lock context.</summary>
    public static ReceiveLockContext Instance { get; } = new NoLockReceiveContext();

    NoLockReceiveContext()
    {
    }

    /// <summary>Completes settlement without contacting a transport.</summary>
    /// <param name="cancellationToken">Cancels settlement.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Completes fault settlement without contacting a transport.</summary>
    /// <param name="exception">The delivery failure being settled.</param>
    /// <param name="cancellationToken">Cancels settlement.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Confirms lock validity for a transport that has no receive lock.</summary>
    /// <param name="cancellationToken">Cancels validation.</param>
    /// <returns>A completed task, or a canceled task when cancellation was requested.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;
    }
}
