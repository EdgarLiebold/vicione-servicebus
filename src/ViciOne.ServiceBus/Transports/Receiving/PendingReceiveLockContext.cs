using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Serializes settlement attempts for concurrent deliveries that represent the same message.</summary>
internal sealed class PendingReceiveLockContext :
    ReceiveLockContext
{
    readonly SemaphoreSlim _executionGate = new(1, 1);
    readonly Queue<Lock> _pending = new(1);
    readonly object _stateLock = new();
    bool _executing;
    Lock? _lockContext;

    /// <summary>Gets whether no delivery or settlement operation remains active.</summary>
    public bool IsEmpty
    {
        get
        {
            lock (_stateLock)
                return !_executing && _lockContext == null && _pending.Count == 0;
        }
    }

    /// <summary>Accepts the first valid retained delivery and discards the remaining settlement fallbacks.</summary>
    /// <param name="cancellationToken">The token that cancels settlement.</param>
    /// <returns>A task that completes after one retained transport lock has accepted the delivery.</returns>
    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(context => context.CompleteAsync(cancellationToken), clearAfterSuccess: true, cancellationToken);
    }

    /// <summary>Faults the first valid retained delivery and discards the remaining settlement fallbacks.</summary>
    /// <param name="exception">The receive-pipeline failure used for transport settlement.</param>
    /// <param name="cancellationToken">The token that cancels settlement.</param>
    /// <returns>A task that completes after one retained transport lock has settled the failure.</returns>
    public Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return ExecuteAsync(context => context.FaultedAsync(exception, cancellationToken), clearAfterSuccess: true, cancellationToken);
    }

    /// <summary>Selects the first retained delivery whose transport lock remains valid.</summary>
    /// <param name="cancellationToken">The token that cancels validation.</param>
    /// <returns>A task that completes when a valid retained transport lock has been selected.</returns>
    public Task ValidateLockStatusAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(context => context.ValidateLockStatusAsync(cancellationToken), clearAfterSuccess: false, cancellationToken);
    }

    /// <summary>Adds a delivery lock to the serialized settlement sequence.</summary>
    /// <param name="receiveContext">The delivery associated with the lock.</param>
    /// <param name="receiveLockContext">The transport lock used to settle the delivery.</param>
    /// <returns><see langword="true" /> when this is the delivery that should be dispatched; otherwise, <see langword="false" />.</returns>
    public bool Enqueue(BaseReceiveContext receiveContext, ReceiveLockContext receiveLockContext)
    {
        ArgumentNullException.ThrowIfNull(receiveContext);
        ArgumentNullException.ThrowIfNull(receiveLockContext);

        var lockContext = new Lock(receiveContext, receiveLockContext);
        lock (_stateLock)
        {
            if (!_executing && _lockContext == null && _pending.Count == 0)
            {
                _lockContext = lockContext;
                return true;
            }

            _pending.Enqueue(lockContext);
            return false;
        }
    }

    async Task ExecuteAsync(Func<ReceiveLockContext, Task> action, bool clearAfterSuccess, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _executionGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            lock (_stateLock)
            {
                _executing = true;
                if (_lockContext == null)
                {
                    if (_pending.Count == 0)
                        return;

                    _lockContext = _pending.Dequeue();
                }
            }

            ExceptionDispatchInfo? dispatchInfo = null;

            do
            {
                try
                {
                    Lock? lockContext;
                    lock (_stateLock)
                        lockContext = _lockContext;

                    if (!lockContext.HasValue)
                        return;

                    await action(lockContext.Value.ReceiveLockContext).ConfigureAwait(false);

                    if (clearAfterSuccess)
                    {
                        lock (_stateLock)
                        {
                            _lockContext = null;
                            _pending.Clear();
                        }
                    }

                    return;
                }
                catch (Exception exception)
                {
                    Exception settlementFailure;
                    try
                    {
                        settlementFailure = exception.GetBaseException() ?? exception;
                    }
                    catch
                    {
                        settlementFailure = exception;
                    }

                    dispatchInfo = ExceptionDispatchInfo.Capture(settlementFailure);
                }
            }
            while (TryDequeue());

            (dispatchInfo ?? throw new InvalidOperationException("Lock settlement failed without an exception.")).Throw();
        }
        finally
        {
            lock (_stateLock)
                _executing = false;

            _executionGate.Release();
        }
    }

    bool TryDequeue()
    {
        lock (_stateLock)
        {
            if (_pending.Count == 0)
            {
                _lockContext = null;
                return false;
            }

            _lockContext = _pending.Dequeue();
            return true;
        }
    }

    /// <summary>Cancels every delivery retained by the settlement sequence.</summary>
    public void Cancel()
    {
        lock (_stateLock)
        {
            _lockContext?.ReceiveContext.Cancel();
            foreach (Lock pendingLock in _pending)
                pendingLock.ReceiveContext.Cancel();
        }
    }


    readonly record struct Lock(BaseReceiveContext ReceiveContext, ReceiveLockContext ReceiveLockContext);
}
