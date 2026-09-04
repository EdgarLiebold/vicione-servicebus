using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a context message factory implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class ContextMessageFactory<TContext, T>
    where TContext : class, ConsumeContext
    where T : class
{
    readonly Func<TContext, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> _messageFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageFactory">The message factory value.</param>
    public ContextMessageFactory(Func<TContext, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> messageFactory)
    {
        _messageFactory = messageFactory;
    }

    /// <summary>
    /// Gets message.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetMessageAsync(TContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>(cancellationToken); Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> result = _messageFactory(context);
        if (result.Status == TaskStatus.RanToCompletion)
            return result;

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
        {
            return await result.ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    /// <summary>
    /// Performs the use operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UseAsync(TContext context, Func<TContext, global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>, Task> callback, CancellationToken cancellationToken = default)
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> msgTask = GetMessageAsync(context, cancellationToken: cancellationToken);
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(context, msgTask.GetAwaiter().GetResult());

        async Task GetResultAsync()
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await msgTask.ConfigureAwait(false);

            await callback(context, send).ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    /// <summary>
    /// Performs the use operation.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TResult> UseAsync<TResult>(TContext context, Func<TContext, global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>, Task<TResult>> callback, CancellationToken cancellationToken = default)
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> msgTask = GetMessageAsync(context, cancellationToken: cancellationToken);
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(context, msgTask.GetAwaiter().GetResult());

        async Task<TResult> GetResultAsync()
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await msgTask.ConfigureAwait(false);

            return await callback(context, send).ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    /// <summary>
    /// Converts a value to <see cref="ContextMessageFactory&lt;TContext, T&gt;" />.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator ContextMessageFactory<TContext, T>(TaskMessageFactory<T> factory)
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> message = factory.GetMessageAsync();

        return new ContextMessageFactory<TContext, T>(_ => message);
    }
}
