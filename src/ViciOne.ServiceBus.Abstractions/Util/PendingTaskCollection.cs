using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;

/// <summary>Stores a collection of pending task values.</summary>
public class PendingTaskCollection
{
    readonly Dictionary<long, Task> _tasks;
    long _nextId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="capacity">The capacity.</param>
    public PendingTaskCollection(int capacity)
    {
        _tasks = new Dictionary<long, Task>(capacity);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="tasks">The tasks.</param>
    public void Add(IEnumerable<Task> tasks)
    {
        foreach (var task in tasks)
            Add(task);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="task">The task.</param>
    public void Add(Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (task.Status == TaskStatus.RanToCompletion)
            return;

        var id = Interlocked.Increment(ref _nextId);

        lock (_tasks)
            _tasks.Add(id, task);

        task.ContinueWith(x => Remove(id), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CompletedAsync(CancellationToken cancellationToken = default)
    {
        KeyValuePair<long, Task>[] pending;
        do
        {
            lock (_tasks)
            {
                if (_tasks.Count == 0)
                    return;

                pending = [.. _tasks];
            }

            var whenAll = Task.WhenAll(pending.Select(static entry => entry.Value));

            if (cancellationToken.CanBeCanceled)
                whenAll = whenAll.OrCanceledAsync(cancellationToken);

            bool canceledByCaller = false;
            try
            {
                await whenAll.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                canceledByCaller = true;
                throw;
            }
            finally
            {
                if (!canceledByCaller)
                {
                    lock (_tasks)
                    {
                        foreach (KeyValuePair<long, Task> entry in pending)
                            _tasks.Remove(entry.Key);
                    }
                }
            }
        }
        while (pending.Length > 0);
    }

    void Remove(long id)
    {
        lock (_tasks)
            _tasks.Remove(id);
    }
}
