using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transactions;

internal sealed class AmbientTransactionNotification :
    IEnlistmentNotification
{
    readonly List<Func<CancellationToken, Task>> _pendingActions;
    int _state;

    public AmbientTransactionNotification()
    {
        _pendingActions = new List<Func<CancellationToken, Task>>();
    }

    public void Prepare(PreparingEnlistment preparingEnlistment)
    {
        ArgumentNullException.ThrowIfNull(preparingEnlistment);

        LogContext.Debug?.Log("Prepare notification received");

        try
        {
            ExecutePendingActions();
            preparingEnlistment.Prepared();
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "ViciOne.ServiceBus: Error executing pending actions");
            preparingEnlistment.ForceRollback(exception);
        }
    }

    public void Commit(Enlistment enlistment)
    {
        Complete(enlistment, "Commit notification received");
    }

    public void Rollback(Enlistment enlistment)
    {
        Complete(enlistment, "Rollback notification received");
    }

    public void InDoubt(Enlistment enlistment)
    {
        Complete(enlistment, "In doubt notification received");
    }

    public void Add(Func<CancellationToken, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (_pendingActions)
        {
            if (_state != 0)
                throw new InvalidOperationException("The ambient transaction is no longer accepting bus actions.");

            _pendingActions.Add(action);
        }
    }

    void Complete(Enlistment enlistment, string message)
    {
        ArgumentNullException.ThrowIfNull(enlistment);

        LogContext.Debug?.Log(message);

        lock (_pendingActions)
        {
            _state = 2;
            _pendingActions.Clear();
        }

        enlistment.Done();
    }

    void ExecutePendingActions()
    {
        Func<CancellationToken, Task>[] pendingActions;
        lock (_pendingActions)
        {
            if (_state != 0)
                throw new InvalidOperationException("The ambient transaction bus actions have already been prepared.");

            _state = 1;
            pendingActions = _pendingActions.ToArray();
            _pendingActions.Clear();
        }

        foreach (Func<CancellationToken, Task> action in pendingActions)
            TaskBlocking.Wait(() => action(CancellationToken.None));

        lock (_pendingActions)
            _state = 2;
    }
}
