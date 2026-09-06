using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Explicit synchronous boundary for synchronous host APIs. Product code should prefer async
/// end-to-end; callers of this type deliberately accept blocking and must not rely on a blocked
/// synchronization context for completion.
/// </summary>
public static class TaskBlocking
{
    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <param name="taskFactory">The task factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static void Wait(Func<Task> taskFactory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        Task task = taskFactory() ?? throw new InvalidOperationException("The task factory must return a Task.");
        Wait(task, cancellationToken);
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <param name="task">The task value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static void Wait(Task task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (cancellationToken.CanBeCanceled)
            task = task.OrCanceledAsync(cancellationToken);

        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Performs the wait operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="taskFactory">The task factory value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static T Wait<T>(Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        Task<T> task = taskFactory() ?? throw new InvalidOperationException("The task factory must return a Task.");
        if (cancellationToken.CanBeCanceled)
            task = task.OrCanceledAsync(cancellationToken);

        return task.GetAwaiter().GetResult();
    }
}
