using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes test consume message events.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class TestConsumeMessageObserver<T> :
    IConsumeMessageObserver<T>
    where T : class
{
    readonly TaskCompletionSource<T> _consumeFaulted;
    readonly TaskCompletionSource<T> _postConsumed;
    readonly TaskCompletionSource<T> _preConsumed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="preConsumed">The pre consumed.</param>
    /// <param name="postConsumed">The post consumed.</param>
    /// <param name="consumeFaulted">The consume faulted.</param>
    public TestConsumeMessageObserver(TaskCompletionSource<T> preConsumed, TaskCompletionSource<T> postConsumed,
        TaskCompletionSource<T> consumeFaulted)
    {
        _preConsumed = preConsumed;
        _postConsumed = postConsumed;
        _consumeFaulted = consumeFaulted;
    }

    /// <summary>Gets the pre consumed.</summary>
    public Task<T> PreConsumed => _preConsumed.Task;
    /// <summary>Gets the post consumed.</summary>
    public Task<T> PostConsumed => _postConsumed.Task;
    /// <summary>Gets the consume faulted.</summary>
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
