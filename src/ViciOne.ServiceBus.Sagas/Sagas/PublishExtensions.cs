using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for publish.</summary>
public static class PublishExtensions
{
    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Publish<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        TMessage message, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new PublishActivity<TInstance, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> PublishAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        Task<TMessage> message, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new PublishActivity<TInstance, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Publish<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventMessageFactory<TInstance, TMessage> messageFactory, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new PublishActivity<TInstance, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> PublishAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        AsyncEventMessageFactory<TInstance, TMessage> messageFactory, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new PublishActivity<TInstance, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> PublishAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new PublishActivity<TInstance, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Publish<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        TMessage message, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new PublishActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> PublishAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Task<TMessage> message, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new PublishActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Publish<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new PublishActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> PublishAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory, Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new PublishActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> PublishAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new PublishActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Publish<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, TMessage message,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new FaultedPublishActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> PublishAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, Task<TMessage> message,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new FaultedPublishActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Publish<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedPublishActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> PublishAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedPublishActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> PublishAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedPublishActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Publish<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, TMessage message,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new FaultedPublishActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> PublishAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, Task<TMessage> message,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new FaultedPublishActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(message, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Publish<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedPublishActivity<TInstance, TData, TException, TMessage>(
            MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> PublishAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedPublishActivity<TInstance, TData, TException, TMessage>(
            MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> PublishAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<PublishContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedPublishActivity<TInstance, TData, TException, TMessage>(
            MessageFactory<TMessage>.Create(messageFactory, Uplift(callback))));
    }

    static Action<SendContext<T>>? Uplift<T>(Action<PublishContext<T>>? callback)
        where T : class
    {
        if (callback == null)
            return null;

        return context =>
        {
            var payload = context.GetPayload<PublishContext<T>>();

            callback(payload);
        };
    }
}
