using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a test consume message observer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TestConsumeMessageObserver<T> :
    IConsumeMessageObserver<T>
    where T : class
{
    readonly TaskCompletionSource<T> _consumeFaulted;
    readonly TaskCompletionSource<T> _postConsumed;
    readonly TaskCompletionSource<T> _preConsumed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="preConsumed">The pre consumed value.</param>
    /// <param name="postConsumed">The post consumed value.</param>
    /// <param name="consumeFaulted">The consume faulted value.</param>
    public TestConsumeMessageObserver(TaskCompletionSource<T> preConsumed, TaskCompletionSource<T> postConsumed,
        TaskCompletionSource<T> consumeFaulted)
    {
        _preConsumed = preConsumed;
        _postConsumed = postConsumed;
        _consumeFaulted = consumeFaulted;
    }

    /// <summary>
    /// Gets the pre consumed value.
    /// </summary>
    public Task<T> PreConsumed => _preConsumed.Task;
    /// <summary>
    /// Gets the post consumed value.
    /// </summary>
    public Task<T> PostConsumed => _postConsumed.Task;
    /// <summary>
    /// Gets the consume faulted value.
    /// </summary>
    public Task<T> ConsumeFaulted => _consumeFaulted.Task;

    Task IConsumeMessageObserver<T>.PreConsumeAsync(ConsumeContext<T> context)
    {
        _preConsumed.TrySetResult(context.Message);

        return Task.CompletedTask;
    }

    Task IConsumeMessageObserver<T>.PostConsumeAsync(ConsumeContext<T> context)
    {
        _postConsumed.TrySetResult(context.Message);

        return Task.CompletedTask;
    }

    Task IConsumeMessageObserver<T>.ConsumeFaultAsync(ConsumeContext<T> context, Exception exception)
    {
        _consumeFaulted.TrySetException(exception);

        return Task.CompletedTask;
    }
}
