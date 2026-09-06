using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Creates completion sources with asynchronous continuations, which is the required default for
/// infrastructure state transitions to avoid running arbitrary continuations while holding locks.
/// </summary>
public static class TaskCompletionSources
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The newly created instance.</returns>
    public static TaskCompletionSource Create(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Creates the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The newly created instance.</returns>
    public static TaskCompletionSource<T> Create<T>(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
