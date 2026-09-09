using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Coordinates collector admissions, terminal batch flushing, and executor shutdown.</summary>
internal sealed class BatchCollectorLifetime
{
    readonly object _lock = new();
    int _activeOperationCount;
    TaskCompletionSource? _operationDrain;
    Task? _disposeTask;

    /// <summary>Creates the serialized collection executor and bounded batch dispatcher.</summary>
    /// <param name="dispatcherConcurrencyLimit">The number of batches that may be delivered concurrently.</param>
    public BatchCollectorLifetime(int dispatcherConcurrencyLimit)
    {
        Collector = new TaskExecutor();
        Dispatcher = new TaskExecutor(dispatcherConcurrencyLimit);
    }

    /// <summary>Gets the executor that serializes changes to active batches.</summary>
    public TaskExecutor Collector { get; }

    /// <summary>Gets the executor that bounds concurrent batch delivery.</summary>
    public TaskExecutor Dispatcher { get; }

    /// <summary>Attempts to admit an operation before terminal disposal begins.</summary>
    /// <returns><see langword="true" /> when the operation is owned by this lifetime.</returns>
    public bool TryBeginOperation()
    {
        lock (_lock)
        {
            if (_disposeTask != null)
                return false;

            _activeOperationCount++;
            return true;
        }
    }

    /// <summary>Releases one admitted operation and signals a pending terminal drain when it was the last.</summary>
    public void CompleteOperation()
    {
        TaskCompletionSource? operationDrain = null;
        lock (_lock)
        {
            _activeOperationCount--;
            if (_activeOperationCount == 0)
                operationDrain = _operationDrain;
        }

        operationDrain?.TrySetResult();
    }

    /// <summary>Stops admissions, flushes every active batch, and drains both executors exactly once.</summary>
    /// <param name="flushActiveBatches">The serialized operation that closes all active batches.</param>
    /// <returns>A value task that completes after all admitted work and deliveries have terminated.</returns>
    public ValueTask DisposeAsync(Func<Task> flushActiveBatches)
    {
        ArgumentNullException.ThrowIfNull(flushActiveBatches);

        lock (_lock)
        {
            if (_disposeTask != null)
                return new ValueTask(_disposeTask);

            _operationDrain = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (_activeOperationCount == 0)
                _operationDrain.TrySetResult();

            _disposeTask = DisposeCoreAsync(_operationDrain.Task, flushActiveBatches);
            return new ValueTask(_disposeTask);
        }
    }

    async Task DisposeCoreAsync(Task operationDrain, Func<Task> flushActiveBatches)
    {
        await operationDrain.ConfigureAwait(false);

        List<Exception>? failures = null;
        await CaptureFailureAsync(
            () => Collector.ExecuteAsync(flushActiveBatches),
            exception => (failures ??= []).Add(exception)).ConfigureAwait(false);
        await CaptureFailureAsync(
            () => Dispatcher.DisposeAsync().AsTask(),
            exception => (failures ??= []).Add(exception)).ConfigureAwait(false);
        await CaptureFailureAsync(
            () => Collector.DisposeAsync().AsTask(),
            exception => (failures ??= []).Add(exception)).ConfigureAwait(false);

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException("Batch collector disposal encountered multiple failures.", failures);
    }

    static async Task CaptureFailureAsync(Func<Task> operation, Action<Exception> capture)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            capture(exception);
        }
    }
}
