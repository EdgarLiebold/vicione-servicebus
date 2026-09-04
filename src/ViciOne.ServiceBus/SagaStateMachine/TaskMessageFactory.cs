using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class TaskMessageFactory<T>
    where T : class
{
    readonly Task<SendTuple<T>> _messageFactory;

    public TaskMessageFactory(Task<SendTuple<T>> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    public Task<SendTuple<T>> GetMessageAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.SendTuple<T>>(cancellationToken); return _messageFactory;
    }

    public Task UseAsync(Func<SendTuple<T>, Task> callback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Task<SendTuple<T>> msgTask = _messageFactory;
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(msgTask.GetAwaiter().GetResult());

        async Task GetResultAsync()
        {
            SendTuple<T> send = await msgTask.ConfigureAwait(false);

            await callback(send).ConfigureAwait(false);
        }

        return GetResultAsync();
    }
}
