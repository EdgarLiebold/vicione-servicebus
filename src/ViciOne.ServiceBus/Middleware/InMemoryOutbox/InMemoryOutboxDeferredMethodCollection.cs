using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Owns the ordered operations deferred by one in-memory outbox.</summary>
internal sealed class InMemoryOutboxDeferredMethodCollection
{
    readonly Task? _clearToSend;
    readonly List<InMemoryOutboxDeferredMethod> _pendingMethods;

    /// <summary>Initializes an empty deferred-operation collection.</summary>
    /// <param name="clearToSend">An optional task whose completion permits immediate execution of later additions.</param>
    public InMemoryOutboxDeferredMethodCollection(Task? clearToSend = null)
    {
        _clearToSend = clearToSend;
        _pendingMethods = [];
    }

    /// <summary>Queues an operation or executes it immediately after the outbox has been released.</summary>
    /// <param name="method">The asynchronous operation to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has been queued or immediately executed.</returns>
    public Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (_clearToSend?.IsCompleted ?? false)
            return method() ?? throw new InvalidOperationException("The deferred outbox operation returned a null task.");

        var executionContext = ExecutionContext.Capture();

        lock (_pendingMethods)
        {
            _pendingMethods.Add(new InMemoryOutboxDeferredMethod(executionContext, method));

            return Task.CompletedTask;
        }
    }

    internal int CreateCheckpoint()
    {
        lock (_pendingMethods)
            return _pendingMethods.Count;
    }

    /// <summary>Removes and executes all queued operations.</summary>
    /// <param name="concurrent">Whether independent operations may execute concurrently.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the removed operations have finished.</returns>
    public async Task ExecuteAsync(bool concurrent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InMemoryOutboxDeferredMethod[] pendingActions;
        lock (_pendingMethods)
        {
            pendingActions = _pendingMethods.ToArray();
            _pendingMethods.Clear();
        }

        try
        {
            if (pendingActions.Length > 0)
            {
                if (concurrent)
                {
                    var collection = new PendingTaskCollection(pendingActions.Length);

                    collection.Add(pendingActions.Select(method => method.RunAsync(cancellationToken: cancellationToken)));

                    await collection.CompletedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    foreach (var method in pendingActions)
                        await method.RunAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            foreach (var deferredMethod in pendingActions)
                deferredMethod.Dispose();
        }
    }

    /// <summary>Removes all queued operations without executing them.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void Discard(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InMemoryOutboxDeferredMethod[] pendingMethods;
        lock (_pendingMethods)
        {
            pendingMethods = _pendingMethods.ToArray();
            _pendingMethods.Clear();
        }

        foreach (var method in pendingMethods)
            method.Dispose();
    }

    internal void DiscardSince(int checkpoint)
    {
        InMemoryOutboxDeferredMethod[] pendingMethods;
        lock (_pendingMethods)
        {
            if (checkpoint < 0 || checkpoint > _pendingMethods.Count)
                throw new ArgumentOutOfRangeException(nameof(checkpoint));

            int count = _pendingMethods.Count - checkpoint;
            if (count == 0)
                return;

            pendingMethods = _pendingMethods.GetRange(checkpoint, count).ToArray();
            _pendingMethods.RemoveRange(checkpoint, count);
        }

        foreach (var method in pendingMethods)
            method.Dispose();
    }
}
