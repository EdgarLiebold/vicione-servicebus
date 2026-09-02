namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// Connection-lifetime owner for durable transport resources. Creation is single-flight per key and caller cancellation
/// cancels only that caller's wait. The store lifetime owns resource creation and disposal.
/// </summary>
sealed class DurableResourceStore<TKey, TValue> :
    IAsyncDisposable
    where TKey : notnull
    where TValue : class
{
    readonly CancellationTokenSource _lifetimeCancellationSource;
    readonly Dictionary<TKey, Entry> _resources;
    readonly object _sync = new();
    TaskCompletionSource? _disposeCompletion;
    bool _disposed;

    public DurableResourceStore(CancellationToken lifetimeCancellationToken, IEqualityComparer<TKey>? comparer = null)
    {
        _lifetimeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellationToken);
        _resources = new Dictionary<TKey, Entry>(comparer);
    }

    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        lock (_sync)
        {
            ThrowIfDisposed_NoLock();

            if (_resources.TryGetValue(key, out var entry) && entry.Completion.Task.IsCompletedSuccessfully)
            {
                value = entry.Completion.Task.Result;
                return true;
            }
        }

        value = null;
        return false;
    }

    public Task<TValue> GetOrAddAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Entry entry;
        bool start;

        lock (_sync)
        {
            ThrowIfDisposed_NoLock();

            if (_resources.TryGetValue(key, out entry!))
                start = false;
            else
            {
                entry = new Entry(_lifetimeCancellationSource.Token);
                _resources.Add(key, entry);
                start = true;
            }
        }

        if (start)
            _ = CompleteCreationAsync(key, entry, factory);

        return entry.Completion.Task.WaitAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(TKey key)
    {
        Entry? entry;
        bool cancelCreation;
        lock (_sync)
        {
            ThrowIfDisposed_NoLock();

            if (!_resources.Remove(key, out entry))
                return false;

            entry.Removed = true;
            cancelCreation = !entry.OwnershipReleased.Task.IsCompleted;
        }

        if (cancelCreation)
            CancelSafely(entry.CreationCancellationSource, "Durable resource creation cancellation faulted during removal");

        await entry.OwnershipReleased.Task.ConfigureAwait(false);

        if (entry.Completion.Task.IsCompletedSuccessfully)
            await DisposeResourceSafelyAsync(entry.Completion.Task.Result).ConfigureAwait(false);

        return true;
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource completion;
        Entry[] entries;

        lock (_sync)
        {
            if (_disposeCompletion is not null)
                return new ValueTask(_disposeCompletion.Task);

            _disposed = true;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeCompletion = completion;
            entries = _resources.Values.ToArray();
            _resources.Clear();

            foreach (var entry in entries)
                entry.Removed = true;
        }

        _ = CompleteDisposalAsync(entries, completion);
        return new ValueTask(completion.Task);
    }

    async Task CompleteDisposalAsync(Entry[] entries, TaskCompletionSource completion)
    {
        try
        {
            CancelSafely(_lifetimeCancellationSource, "Durable resource lifetime cancellation faulted during disposal");
            foreach (var entry in entries)
            {
                if (!entry.OwnershipReleased.Task.IsCompleted)
                    CancelSafely(entry.CreationCancellationSource, "Durable resource creation cancellation faulted during disposal");
            }

            Task[] ownership = entries.Select(x => x.OwnershipReleased.Task).ToArray();
            if (ownership.Length > 0)
                await Task.WhenAll(ownership).ConfigureAwait(false);

            foreach (var entry in entries)
            {
                if (entry.Completion.Task.IsCompletedSuccessfully)
                    await DisposeResourceSafelyAsync(entry.Completion.Task.Result).ConfigureAwait(false);
            }

            _lifetimeCancellationSource.Dispose();
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    async Task CompleteCreationAsync(TKey key, Entry entry, Func<TKey, CancellationToken, ValueTask<TValue>> factory)
    {
        TValue? value = null;
        try
        {
            value = await factory(key, entry.CreationCancellationSource.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The durable resource factory returned null.");

            bool removed;
            lock (_sync)
                removed = _disposed || entry.Removed || !_resources.TryGetValue(key, out var current) || !ReferenceEquals(current, entry);

            if (removed)
            {
                await DisposeResourceSafelyAsync(value).ConfigureAwait(false);
                entry.Completion.TrySetCanceled(entry.CreationCancellationSource.Token);
                return;
            }

            entry.Completion.TrySetResult(value);
        }
        catch (OperationCanceledException) when (entry.CreationCancellationSource.IsCancellationRequested)
        {
            if (value is not null)
                await DisposeResourceSafelyAsync(value).ConfigureAwait(false);

            entry.Completion.TrySetCanceled(entry.CreationCancellationSource.Token);
        }
        catch (Exception exception)
        {
            if (value is not null)
                await DisposeResourceSafelyAsync(value).ConfigureAwait(false);

            lock (_sync)
            {
                if (_resources.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                    _resources.Remove(key);
            }

            entry.Completion.TrySetException(exception);
        }
        finally
        {
            entry.OwnershipReleased.TrySetResult();
            entry.CreationCancellationSource.Dispose();
        }
    }

    static async ValueTask DisposeResourceAsync(TValue value)
    {
        switch (value)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    static async ValueTask DisposeResourceSafelyAsync(TValue value)
    {
        try
        {
            await DisposeResourceAsync(value).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Durable resource disposal faulted");
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
            LogContext.Warning?.Log(exception, faultMessage);
        }
    }

    void ThrowIfDisposed_NoLock()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    sealed class Entry
    {
        public Entry(CancellationToken lifetimeCancellationToken)
        {
            CreationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellationToken);
        }

        public TaskCompletionSource<TValue> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource OwnershipReleased { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenSource CreationCancellationSource { get; }
        public bool Removed { get; set; }
    }
}
