using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Creates context message instances.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class ContextMessageFactory<TContext, T>
    where TContext : class, ConsumeContext
    where T : class
{
    readonly Func<TContext, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> _messageFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageFactory">The message factory.</param>
    public ContextMessageFactory(Func<TContext, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);

        _messageFactory = messageFactory;
    }

    /// <summary>Gets message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetMessageAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>(cancellationToken);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> result = _messageFactory(context)
            ?? throw new InvalidOperationException("The message factory returned no task.");
        if (result.Status == TaskStatus.RanToCompletion)
            return result;

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
        {
            return await result.ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    /// <summary>Applies the selected configuration.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UseAsync(TContext context, Func<TContext, global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>, Task> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(callback);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> msgTask = GetMessageAsync(context, cancellationToken: cancellationToken);
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(context, msgTask.GetAwaiter().GetResult())
                ?? throw new InvalidOperationException("The callback returned no task.");

        async Task GetResultAsync()
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await msgTask.ConfigureAwait(false);

            Task callbackTask = callback(context, send)
                ?? throw new InvalidOperationException("The callback returned no task.");

            await callbackTask.ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    /// <summary>Applies the selected configuration.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the use outcome.</returns>
    public Task<TResult> UseAsync<TResult>(TContext context, Func<TContext, global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>, Task<TResult>> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(callback);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> msgTask = GetMessageAsync(context, cancellationToken: cancellationToken);
        if (msgTask.Status == TaskStatus.RanToCompletion)
            return callback(context, msgTask.GetAwaiter().GetResult())
                ?? throw new InvalidOperationException("The callback returned no task.");

        async Task<TResult> GetResultAsync()
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> send = await msgTask.ConfigureAwait(false);

            Task<TResult> callbackTask = callback(context, send)
                ?? throw new InvalidOperationException("The callback returned no task.");

            return await callbackTask.ConfigureAwait(false);
        }

        return GetResultAsync();
    }

    /// <summary>Converts a value to <see cref="ContextMessageFactory&lt;TContext, T&gt;" />.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public static implicit operator ContextMessageFactory<TContext, T>(TaskMessageFactory<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> message = factory.GetMessageAsync();

        return new ContextMessageFactory<TContext, T>(_ => message);
    }
}
