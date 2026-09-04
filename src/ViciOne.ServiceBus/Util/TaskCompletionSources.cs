using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Creates completion sources with asynchronous continuations, which is the required default for
/// infrastructure state transitions to avoid running arbitrary continuations while holding locks.
/// </summary>
public static class TaskCompletionSources
{
    public static TaskCompletionSource Create(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public static TaskCompletionSource<T> Create<T>(TaskCreationOptions options = TaskCreationOptions.None)
    {
        return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
