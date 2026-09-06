using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching;

public sealed partial class ResourceCache<TValue>
    where TValue : class
{
    ValueTask NotifyAddedAsync(TValue value, CancellationToken cancellationToken)
    {
        return DispatchObserversAsync(observer => observer.ResourceAddedAsync(value, cancellationToken),
            "Resource cache observer faulted after resource add");
    }

    ValueTask NotifyRemovedAsync(TValue value, CancellationToken cancellationToken)
    {
        return DispatchObserversAsync(observer => observer.ResourceRemovedAsync(value, cancellationToken),
            "Resource cache observer faulted after resource removal");
    }

    ValueTask NotifyClearedAsync(CancellationToken cancellationToken)
    {
        return DispatchObserversAsync(observer => observer.CacheClearedAsync(cancellationToken),
            "Resource cache observer faulted after cache clear");
    }

    async ValueTask DispatchObserversAsync(Func<IResourceCacheObserver<TValue>, ValueTask> callback, string faultMessage)
    {
        // Observer callbacks are serialized and backpressure the committing caller. Rejecting re-entry before
        // any state change prevents deadlock without introducing an unbounded notification queue.
        await _observerDispatchGate.WaitAsync().ConfigureAwait(false);
        ObserverDispatchScope? previousScope = _observerDispatchScope.Value;
        var currentScope = new ObserverDispatchScope();
        _observerDispatchScope.Value = currentScope;
        try
        {
            foreach (var observer in SnapshotObservers())
            {
                try
                {
                    await callback(observer).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    // Observation is downstream of an irreversible commit and cannot roll it back.
                    LogContext.Warning?.Log(exception, faultMessage);
                }
            }
        }
        finally
        {
            currentScope.Active = false;
            _observerDispatchScope.Value = previousScope;
            _observerDispatchGate.Release();
        }
    }

    IResourceCacheObserver<TValue>[] SnapshotObservers()
    {
        lock (_sync)
            return _observers.ToArray();
    }

    void Disconnect(IResourceCacheObserver<TValue> observer)
    {
        lock (_sync)
            _observers.Remove(observer);
    }

    sealed class ObserverConnectHandle : ConnectHandle
    {
        ResourceCache<TValue>? _cache;
        IResourceCacheObserver<TValue>? _observer;

        public ObserverConnectHandle(ResourceCache<TValue> cache, IResourceCacheObserver<TValue> observer)
        {
            _cache = cache;
            _observer = observer;
        }

        public void Disconnect()
        {
            var cache = Interlocked.Exchange(ref _cache, null);
            var observer = Interlocked.Exchange(ref _observer, null);

            if (cache is not null && observer is not null)
                cache.Disconnect(observer);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    sealed class ObserverDispatchScope
    {
        public bool Active { get; set; } = true;
    }
}
