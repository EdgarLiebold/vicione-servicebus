using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a pending task collection implementation.
/// </summary>
public class PendingTaskCollection
{
    readonly Dictionary<long, Task> _tasks;
    long _nextId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="capacity">The capacity value.</param>
    public PendingTaskCollection(int capacity)
    {
        _tasks = new Dictionary<long, Task>(capacity);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="tasks">The tasks value.</param>
    public void Add(IEnumerable<Task> tasks)
    {
        foreach (var task in tasks)
            Add(task);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="task">The task value.</param>
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

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CompletedAsync(CancellationToken cancellationToken = default)
    {
        Task[] tasks;
        do
        {
            lock (_tasks)
            {
                if (_tasks.Count == 0)
                    return;

                tasks = new Task[_tasks.Count];
                _tasks.Values.CopyTo(tasks, 0);

                _tasks.Clear();
            }

            var whenAll = Task.WhenAll(tasks);

            if (cancellationToken.CanBeCanceled)
                whenAll = whenAll.OrCanceledAsync(cancellationToken);

            await whenAll.ConfigureAwait(false);
        }
        while (tasks.Length > 0);
    }

    void Remove(long id)
    {
        lock (_tasks)
            _tasks.Remove(id);
    }
}
