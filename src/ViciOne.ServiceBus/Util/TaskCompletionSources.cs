using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Creates completion sources with asynchronous continuations, which is the required default for
/// infrastructure state transitions to avoid running arbitrary continuations while holding locks.
/// </summary>
public static class TaskCompletionSources
{
    /// <summary>Creates a non-generic completion source whose continuations run asynchronously.</summary>
    /// <param name="options">Additional task-creation options.</param>
    /// <returns>A completion source configured with the supplied options and asynchronous continuations.</returns>
    public static TaskCompletionSource Create(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Creates a completion source whose continuations run asynchronously.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="options">Additional task-creation options.</param>
    /// <returns>A completion source configured with the supplied options and asynchronous continuations.</returns>
    public static TaskCompletionSource<T> Create<T>(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
