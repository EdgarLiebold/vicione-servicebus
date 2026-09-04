using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class ContextMessageFactory<TContext, T>
    where TContext : class, ConsumeContext
    where T : class
{
    readonly Func<TContext, Task<SendTuple<T>>> _messageFactory;

    public ContextMessageFactory(Func<TContext, Task<SendTuple<T>>> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    public Task<SendTuple<T>> GetMessageAsync(TContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.SendTuple<T>>(cancellationToken); Task<SendTuple<T>> result = _messageFactory(context);
        if (result.Status == TaskStatus.RanToCompletion)
            return result;

        async Task<SendTuple<T>> GetResultAsync()
        {
            return await result.ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    public Task UseAsync(TContext context, Func<TContext, SendTuple<T>, Task> callback, CancellationToken cancellationToken = default)
    {
        Task<SendTuple<T>> msgTask = GetMessageAsync(context, cancellationToken: cancellationToken);
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(context, msgTask.GetAwaiter().GetResult());

        async Task GetResultAsync()
        {
            SendTuple<T> send = await msgTask.ConfigureAwait(false);

            await callback(context, send).ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    public Task<TResult> UseAsync<TResult>(TContext context, Func<TContext, SendTuple<T>, Task<TResult>> callback, CancellationToken cancellationToken = default)
    {
        Task<SendTuple<T>> msgTask = GetMessageAsync(context, cancellationToken: cancellationToken);
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(context, msgTask.GetAwaiter().GetResult());

        async Task<TResult> GetResultAsync()
        {
            SendTuple<T> send = await msgTask.ConfigureAwait(false);

            return await callback(context, send).ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    public static implicit operator ContextMessageFactory<TContext, T>(TaskMessageFactory<T> factory)
    {
        Task<SendTuple<T>> message = factory.GetMessageAsync();

        return new ContextMessageFactory<TContext, T>(_ => message);
    }
}
