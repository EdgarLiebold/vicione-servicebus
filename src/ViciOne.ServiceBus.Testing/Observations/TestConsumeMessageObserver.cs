using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes tasks for the first pre-consume, post-consume, and consume-fault callbacks of one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class TestConsumeMessageObserver<TMessage> :
    IConsumeMessageObserver<TMessage>
    where TMessage : class
{
    readonly TaskCompletionSource<TMessage> _consumeFaulted;
    readonly TaskCompletionSource<TMessage> _postConsumed;
    readonly TaskCompletionSource<TMessage> _preConsumed;

    /// <summary>Creates an observer backed by caller-owned completion sources.</summary>
    /// <param name="preConsumed">The source completed with the message before consumption.</param>
    /// <param name="postConsumed">The source completed with the message after successful consumption.</param>
    /// <param name="consumeFaulted">The source faulted with the consume-pipeline exception.</param>
    public TestConsumeMessageObserver(TaskCompletionSource<TMessage> preConsumed, TaskCompletionSource<TMessage> postConsumed,
        TaskCompletionSource<TMessage> consumeFaulted)
    {
        _preConsumed = preConsumed ?? throw new ArgumentNullException(nameof(preConsumed));
        _postConsumed = postConsumed ?? throw new ArgumentNullException(nameof(postConsumed));
        _consumeFaulted = consumeFaulted ?? throw new ArgumentNullException(nameof(consumeFaulted));
    }

    /// <summary>Gets the task completed with the first message entering its consume pipeline.</summary>
    public Task<TMessage> PreConsumed => _preConsumed.Task;
    /// <summary>Gets the task completed with the first successfully consumed message.</summary>
    public Task<TMessage> PostConsumed => _postConsumed.Task;
    /// <summary>Gets the task faulted by the first failed consumption.</summary>
    public Task<TMessage> ConsumeFaulted => _consumeFaulted.Task;

    Task IConsumeMessageObserver<TMessage>.PreConsumeAsync(ConsumeContext<TMessage> context)
    {
        _preConsumed.TrySetResult(context.Message);

        return Task.CompletedTask;
    }

    Task IConsumeMessageObserver<TMessage>.PostConsumeAsync(ConsumeContext<TMessage> context)
    {
        _postConsumed.TrySetResult(context.Message);

        return Task.CompletedTask;
    }

    Task IConsumeMessageObserver<TMessage>.ConsumeFaultAsync(ConsumeContext<TMessage> context, Exception exception)
    {
        _consumeFaulted.TrySetException(exception);

        return Task.CompletedTask;
    }
}
