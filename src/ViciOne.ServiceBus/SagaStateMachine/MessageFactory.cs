using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a message factory implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public static class MessageFactory<T>
    where T : class
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskMessageFactory<T> Create(T message)
    {
        return new TaskMessageFactory<T>(Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message)));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskMessageFactory<T> Create(T message, IPipe<SendContext<T>> pipe)
    {
        return pipe.IsNotEmpty()
            ? new TaskMessageFactory<T>(Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, pipe)))
            : Create(message);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskMessageFactory<T> Create(T message, Action<SendContext<T>>? callback)
    {
        if (callback == null)
            return Create(message);

        IPipe<SendContext<T>> callbackPipe = Pipe.Execute(callback);

        return new TaskMessageFactory<T>(Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, callbackPipe)));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskMessageFactory<T> Create(Task<T> factory)
    {
        if (factory.Status == TaskStatus.RanToCompletion)
            return new TaskMessageFactory<T>(Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(factory.GetAwaiter().GetResult())));

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync()
        {
            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await factory.ConfigureAwait(false));
        }

        return new TaskMessageFactory<T>(FactoryAsync());
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskMessageFactory<T> Create(Task<T> factory, IPipe<SendContext<T>> pipe)
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        if (factory.Status == TaskStatus.RanToCompletion)
            return new TaskMessageFactory<T>(Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(factory.GetAwaiter().GetResult(), pipe)));

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync()
        {
            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await factory.ConfigureAwait(false), pipe);
        }

        return new TaskMessageFactory<T>(FactoryAsync());
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static TaskMessageFactory<T> Create(Task<T> factory, Action<SendContext<T>>? callback)
    {
        if (callback == null)
            return Create(factory);

        IPipe<SendContext<T>> callbackPipe = Pipe.Execute(callback);

        if (factory.Status == TaskStatus.RanToCompletion)
            return new TaskMessageFactory<T>(Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(factory.GetAwaiter().GetResult(), callbackPipe)));

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync()
        {
            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await factory.ConfigureAwait(false), callbackPipe);
        }

        return new TaskMessageFactory<T>(FactoryAsync());
    }

    // Saga/Message

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(T message,
        SendContextCallback<TSaga, TMessage, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return callback == null
            ? Create(message)
            : Create(context => message, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(Task<T> factory,
        SendContextCallback<TSaga, TMessage, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return callback == null
            ? Create(factory)
            : Create(context => factory, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(
        Func<BehaviorContext<TSaga, TMessage>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(factory);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(
        Func<BehaviorContext<TSaga, TMessage>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            (var message, IPipe<SendContext<T>> sendPipe) = await factory(context).ConfigureAwait(false);
            if (sendPipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = sendPipe.AddCallback(callback);
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, Pipe.Execute(callback));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(
        Func<BehaviorContext<TSaga, TMessage>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory, SendContextCallback<TSaga, TMessage, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> result = await factory(context).ConfigureAwait(false);
            if (result.Pipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = result.Pipe.AddCallback(ctx => callback(context, ctx));
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx)));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(AsyncEventMessageFactory<TSaga, TMessage, T> factory)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult()));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false));
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(AsyncEventMessageFactory<TSaga, TMessage, T> factory,
        IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), pipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), pipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(AsyncEventMessageFactory<TSaga, TMessage, T> factory,
        Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(AsyncEventMessageFactory<TSaga, TMessage, T> factory,
        SendContextCallback<TSaga, TMessage, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), callbackPipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), callbackPipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(EventMessageFactory<TSaga, TMessage, T> factory)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(EventMessageFactory<TSaga, TMessage, T> factory,
        IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, pipe));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(EventMessageFactory<TSaga, TMessage, T> factory,
        Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> Create<TSaga, TMessage>(EventMessageFactory<TSaga, TMessage, T> factory,
        SendContextCallback<TSaga, TMessage, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga, TMessage> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, callbackPipe));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(T message,
        SendExceptionContextCallback<TSaga, TMessage, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return callback == null
            ? Create(message)
            : Create(context => message, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(Task<T> factory,
        SendExceptionContextCallback<TSaga, TMessage, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return callback == null
            ? Create(factory)
            : Create(context => factory, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(factory);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> result = await factory(context).ConfigureAwait(false);
            if (result.Pipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = result.Pipe.AddCallback(callback);
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, Pipe.Execute(callback));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory,
        SendExceptionContextCallback<TSaga, TMessage, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> result = await factory(context).ConfigureAwait(false);
            if (result.Pipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = result.Pipe.AddCallback(ctx => callback(context, ctx));
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx)));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TMessage, TException, T> factory)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult()));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false));
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TMessage, TException, T> factory, IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), pipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), pipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TMessage, TException, T> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TMessage, TException, T> factory, SendExceptionContextCallback<TSaga, TMessage, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), callbackPipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), callbackPipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        EventExceptionMessageFactory<TSaga, TMessage, TException, T> factory)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        EventExceptionMessageFactory<TSaga, TMessage, TException, T> factory, IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, pipe));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        EventExceptionMessageFactory<TSaga, TMessage, TException, T> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T> Create<TSaga, TMessage, TException>(
        EventExceptionMessageFactory<TSaga, TMessage, TException, T> factory, SendExceptionContextCallback<TSaga, TMessage, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, callbackPipe));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TMessage, TException>, T>(FactoryAsync);
    }

    // Saga Only

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(T message, SendContextCallback<TSaga, T> callback)
        where TSaga : class, SagaStateMachineInstance
    {
        return callback == null
            ? Create(message)
            : Create(context => message, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(Task<T> factory, SendContextCallback<TSaga, T> callback)
        where TSaga : class, SagaStateMachineInstance
    {
        return callback == null
            ? Create(factory)
            : Create(context => factory, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(T message,
        SendExceptionContextCallback<TSaga, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        return callback == null
            ? Create(message)
            : Create(context => message, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(Task<T> factory,
        SendExceptionContextCallback<TSaga, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        return callback == null
            ? Create(factory)
            : Create(context => factory, callback);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(Func<BehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory)
        where TSaga : class, SagaStateMachineInstance
    {
        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(factory);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(Func<BehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory,
        Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> result = await factory(context).ConfigureAwait(false);
            if (result.Pipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = result.Pipe.AddCallback(callback);
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, Pipe.Execute(callback));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(Func<BehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory,
        SendContextCallback<TSaga, T> callback)
        where TSaga : class, SagaStateMachineInstance
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T> result = await factory(context).ConfigureAwait(false);
            if (result.Pipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = result.Pipe.AddCallback(ctx => callback(context, ctx));
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.Message, Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx)));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(AsyncEventMessageFactory<TSaga, T> factory)
        where TSaga : class, SagaStateMachineInstance
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult()));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false));
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(AsyncEventMessageFactory<TSaga, T> factory,
        IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), pipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), pipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(AsyncEventMessageFactory<TSaga, T> factory,
        Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(AsyncEventMessageFactory<TSaga, T> factory,
        SendContextCallback<TSaga, T> callback)
        where TSaga : class, SagaStateMachineInstance
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), callbackPipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), callbackPipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(EventMessageFactory<TSaga, T> factory)
        where TSaga : class, SagaStateMachineInstance
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(EventMessageFactory<TSaga, T> factory,
        IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, pipe));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(EventMessageFactory<TSaga, T> factory,
        Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorContext<TSaga>, T> Create<TSaga>(EventMessageFactory<TSaga, T> factory,
        SendContextCallback<TSaga, T> callback)
        where TSaga : class, SagaStateMachineInstance
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorContext<TSaga> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, callbackPipe));
        }

        return new ContextMessageFactory<BehaviorContext<TSaga>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        Func<BehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(factory);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        Func<BehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            (var message, IPipe<SendContext<T>> sendPipe) = await factory(context).ConfigureAwait(false);
            if (sendPipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = sendPipe.AddCallback(callback);
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, Pipe.Execute(callback));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        Func<BehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>>> factory, SendExceptionContextCallback<TSaga, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            (var message, IPipe<SendContext<T>> sendPipe) = await factory(context).ConfigureAwait(false);
            if (sendPipe.IsNotEmpty())
            {
                IPipe<SendContext<T>> pipe = sendPipe.AddCallback(ctx => callback(context, ctx));
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, pipe);
            }

            return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(message, Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx)));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TException, T> factory)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult()));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false));
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TException, T> factory, IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), pipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), pipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TException, T> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        AsyncEventExceptionMessageFactory<TSaga, TException, T> factory, SendExceptionContextCallback<TSaga, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            Task<T> result = factory(context);
            if (result.Status == TaskStatus.RanToCompletion)
                return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result.GetAwaiter().GetResult(), callbackPipe));

            async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> GetResultAsync()
            {
                return new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(await result.ConfigureAwait(false), callbackPipe);
            }

            return GetResultAsync();
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        EventExceptionMessageFactory<TSaga, TException, T> factory)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        EventExceptionMessageFactory<TSaga, TException, T> factory, IPipe<SendContext<T>> pipe)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        if (!pipe.IsNotEmpty())
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, pipe));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        EventExceptionMessageFactory<TSaga, TException, T> factory, Action<SendContext<T>>? callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        return callback == null ? Create(factory) : Create(factory, Pipe.Execute(callback));
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="factory">The factory value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T> Create<TSaga, TException>(
        EventExceptionMessageFactory<TSaga, TException, T> factory, SendExceptionContextCallback<TSaga, TException, T> callback)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        if (callback == null)
            return Create(factory);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> FactoryAsync(BehaviorExceptionContext<TSaga, TException> context)
        {
            IPipe<SendContext<T>> callbackPipe = Pipe.Execute<SendContext<T>>(ctx => callback(context, ctx));

            var result = factory(context);
            return Task.FromResult(new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>(result, callbackPipe));
        }

        return new ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, T>(FactoryAsync);
    }
}
