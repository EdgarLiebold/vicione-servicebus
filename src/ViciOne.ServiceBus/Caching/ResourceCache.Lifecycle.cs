using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching.Implementation;

namespace ViciOne.ServiceBus.Caching;

public sealed partial class ResourceCache<TValue>
    where TValue : class
{
    async Task CompleteDisposeAsync(TaskCompletionSource completion)
    {
        try
        {
            DisposeCleanupTimerSafely();

            Task operationsDrained;
            Task cleanupTask;
            lock (_sync)
            {
                operationsDrained = _operationsDrained?.Task ?? Task.CompletedTask;
                cleanupTask = _cleanupTask;
            }

            await Task.WhenAll(operationsDrained, cleanupTask).ConfigureAwait(false);

            ResourceCacheEntry<TValue>[] removed;
            PendingResourceCreation<TValue>[] pending;
            lock (_sync)
            {
                _disposed = true;

                removed = _entries.Values.ToArray();
                foreach (var entry in removed)
                    RemoveEntry_NoLock(entry, false);

                pending = _pendingCreations.ToArray();
                foreach (var creation in pending)
                {
                    creation.Invalidated = true;
                    creation.Index.RemovePending(creation.RequestedKey, creation);
                    creation.Completion.TrySetCanceled(_lifetimeCancellationToken);
                }

                _observers.Clear();
                _indexVersion++;
            }

            await ReleaseEntriesAsync(removed, false, CancellationToken.None).ConfigureAwait(false);

            Task[] ownership = pending.Select(x => x.OwnershipReleased.Task).ToArray();
            if (ownership.Length > 0)
                await Task.WhenAll(ownership).ConfigureAwait(false);

            _observerDispatchGate.Dispose();
            _lifetimeCancellationSource.Dispose();
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    static void CancelSafely(CancellationTokenSource source, string faultMessage)
    {
        try
        {
            source.Cancel();
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, faultMessage);
        }
    }

    void DisposeCleanupTimerSafely()
    {
        try
        {
            _cleanupTimer.Dispose();
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, "Resource cache cleanup timer disposal faulted");
        }
    }

    static void LogWarningSafely(Exception exception, string message)
    {
        try
        {
            LogContext.Warning?.Log(exception, message);
        }
        catch
        {
            // Diagnostic logging cannot change cache state or resource-ownership outcomes.
        }
    }

    OperationLease EnterOperation()
    {
        ThrowIfObserverMutation();

        lock (_sync)
        {
            ThrowIfUnavailable_NoLock();
            _activeOperations++;
            return new OperationLease(this);
        }
    }

    void ThrowIfObserverMutation()
    {
        if (_observerDispatchScope.Value?.Active == true)
            throw new InvalidOperationException("Resource cache observer callbacks must not mutate or dispose the same cache.");
    }

    void ExitOperation()
    {
        TaskCompletionSource? drained = null;
        lock (_sync)
        {
            if (_activeOperations <= 0)
                throw new InvalidOperationException("Resource cache operation accounting underflow.");

            _activeOperations--;
            if (_stopping && _activeOperations == 0)
            {
                drained = _operationsDrained;
                _operationsDrained = null;
            }
        }

        drained?.TrySetResult();
    }

    void ThrowIfUnavailable_NoLock()
    {
        ObjectDisposedException.ThrowIf(_stopping || _disposed, this);
    }

    void ThrowIfDisposed_NoLock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    sealed class OperationLease : IDisposable
    {
        ResourceCache<TValue>? _owner;

        public OperationLease(ResourceCache<TValue> owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.ExitOperation();
        }
    }
}
