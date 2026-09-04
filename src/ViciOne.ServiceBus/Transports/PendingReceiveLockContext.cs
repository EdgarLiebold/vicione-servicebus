using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a pending receive lock context implementation.
/// </summary>
public class PendingReceiveLockContext :
    ReceiveLockContext
{
    Lock? _lockContext;
    Queue<Lock> _pending = null!;

    /// <summary>
    /// Gets the is empty value.
    /// </summary>
    public bool IsEmpty => _lockContext == null && (_pending == null || _pending.Count == 0);

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(context => context.CompleteAsync(cancellationToken: cancellationToken), true);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(context => context.FaultedAsync(exception, cancellationToken: cancellationToken), true);
    }

    /// <summary>
    /// Validates lock status.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(context => context.ValidateLockStatusAsync(cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Performs the enqueue operation.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <param name="receiveLockContext">The receive lock context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Enqueue(BaseReceiveContext receiveContext, ReceiveLockContext receiveLockContext)
    {
        var lockContext = new Lock(receiveContext, receiveLockContext);

        lock (this)
        {
            if (_lockContext == null && (_pending == null || _pending.Count == 0))
            {
                _lockContext = lockContext;
                return true;
            }

            (_pending ??= new Queue<Lock>(1)).Enqueue(lockContext);
            return false;
        }
    }

    async Task ExecuteAsync(Func<ReceiveLockContext, Task> action, bool clearLockContext = false)
    {
        if (_lockContext == null)
        {
            lock (this)
            {
                if (_lockContext == null)
                {
                    if (_pending == null || _pending.Count == 0)
                        return;

                    _lockContext = _pending.Dequeue();
                }
            }
        }

        ExceptionDispatchInfo dispatchInfo;

        do
        {
            try
            {
                var lockContext = _lockContext;
                if (!lockContext.HasValue)
                    return;

                if (clearLockContext)
                    _lockContext = null;

                await action(lockContext.Value.ReceiveLockContext).ConfigureAwait(false);

                return;
            }
            catch (Exception ex)
            {
                dispatchInfo = ExceptionDispatchInfo.Capture(ex.GetBaseException());
            }
        }
        while (TryDequeue());

        if (dispatchInfo != null)
        {
            dispatchInfo.Throw();

            throw dispatchInfo.SourceException;
        }
    }

    bool TryDequeue()
    {
        lock (this)
        {
            if (_pending == null || _pending.Count == 0)
            {
                _lockContext = null;
                return false;
            }

            _lockContext = _pending.Dequeue();
            return true;
        }
    }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        lock (this)
        {
            _lockContext?.ReceiveContext.Cancel();
            if (_pending != null)
            {
                foreach (var pendingLock in _pending)
                    pendingLock.ReceiveContext.Cancel();
            }
        }
    }


    readonly struct Lock
    {
        public readonly BaseReceiveContext ReceiveContext;
        public readonly ReceiveLockContext ReceiveLockContext;

        public Lock(BaseReceiveContext receiveContext, ReceiveLockContext receiveLockContext)
        {
            ReceiveContext = receiveContext;
            ReceiveLockContext = receiveLockContext;
        }
    }
}
