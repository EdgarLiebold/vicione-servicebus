using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class TestConsumeMessageObserver<T> :
    IConsumeMessageObserver<T>
    where T : class
{
    readonly TaskCompletionSource<T> _consumeFaulted;
    readonly TaskCompletionSource<T> _postConsumed;
    readonly TaskCompletionSource<T> _preConsumed;

    public TestConsumeMessageObserver(TaskCompletionSource<T> preConsumed, TaskCompletionSource<T> postConsumed,
        TaskCompletionSource<T> consumeFaulted)
    {
        _preConsumed = preConsumed;
        _postConsumed = postConsumed;
        _consumeFaulted = consumeFaulted;
    }

    public Task<T> PreConsumed => _preConsumed.Task;
    public Task<T> PostConsumed => _postConsumed.Task;
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
