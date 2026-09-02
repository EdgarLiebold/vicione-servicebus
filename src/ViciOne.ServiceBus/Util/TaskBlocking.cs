#nullable enable
namespace ViciOne.ServiceBus.Util;

using System;
using System.Threading;
using System.Threading.Tasks;
using Internals;


/// <summary>
/// Explicit synchronous boundary for legacy/synchronous host APIs. Product code should prefer async
/// end-to-end; callers of this type deliberately accept blocking and must not rely on a blocked
/// synchronization context for completion.
/// </summary>
public static class TaskBlocking
{
    public static void Wait(Func<Task> taskFactory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        Task task = taskFactory() ?? throw new InvalidOperationException("The task factory must return a Task.");
        Wait(task, cancellationToken);
    }

    public static void Wait(Task task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (cancellationToken.CanBeCanceled)
            task = task.OrCanceled(cancellationToken);

        task.GetAwaiter().GetResult();
    }

    public static T Wait<T>(Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        Task<T> task = taskFactory() ?? throw new InvalidOperationException("The task factory must return a Task.");
        if (cancellationToken.CanBeCanceled)
            task = task.OrCanceled(cancellationToken);

        return task.GetAwaiter().GetResult();
    }
}
