using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Creates task message instances.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class TaskMessageFactory<T>
    where T : class
{
    readonly Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> _messageFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageFactory">The message factory.</param>
    public TaskMessageFactory(Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    /// <summary>Gets message.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetMessageAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>(cancellationToken); return _messageFactory;
    }

    /// <summary>Applies the selected configuration.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UseAsync(Func<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>, Task> callback, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> msgTask = _messageFactory;
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(msgTask.GetAwaiter().GetResult());

        async Task GetResultAsync()
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await msgTask.ConfigureAwait(false);

            await callback(send).ConfigureAwait(false);
        }

        return GetResultAsync();
    }
}
