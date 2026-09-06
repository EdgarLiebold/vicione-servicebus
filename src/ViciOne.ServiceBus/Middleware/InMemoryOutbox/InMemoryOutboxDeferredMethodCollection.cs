using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Provides an in memory outbox deferred method collection implementation.
/// </summary>
public class InMemoryOutboxDeferredMethodCollection
{
    readonly Task? _clearToSend;
    readonly List<InMemoryOutboxDeferredMethod> _pendingMethods;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="clearToSend">The clear to send value.</param>
    public InMemoryOutboxDeferredMethodCollection(Task? clearToSend = null)
    {
        _clearToSend = clearToSend;
        _pendingMethods = [];
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="method">The method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (_clearToSend?.IsCompleted ?? false)
            return method();

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

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="concurrent">The concurrent value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(bool concurrent, CancellationToken cancellationToken = default)
    {
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
                    {
                        var task = method.RunAsync(cancellationToken: cancellationToken);
                        if (task != null)
                            await task.ConfigureAwait(false);
                    }
                }
            }
        }
        finally
        {
            foreach (var deferredMethod in pendingActions)
                deferredMethod.Dispose();
        }
    }

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void Discard(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); InMemoryOutboxDeferredMethod[] pendingMethods;
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
