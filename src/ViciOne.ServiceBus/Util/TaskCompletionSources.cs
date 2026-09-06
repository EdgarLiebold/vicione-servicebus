using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Creates completion sources with asynchronous continuations, which is the required default for
/// infrastructure state transitions to avoid running arbitrary continuations while holding locks.
/// </summary>
public static class TaskCompletionSources
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskCompletionSource Create(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskCompletionSource<T> Create<T>(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
