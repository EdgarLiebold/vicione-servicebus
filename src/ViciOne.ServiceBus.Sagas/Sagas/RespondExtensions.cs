using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for respond.</summary>
public static class RespondExtensions
{
    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Respond<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        TMessage message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RespondAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Task<TMessage> message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Respond<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RespondAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RespondAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new RespondActivity<TInstance, TData, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Respond<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> RespondAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Respond<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> RespondAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedRespondActivity<TInstance, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Respond<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> RespondAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(message);

        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Respond<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> RespondAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(messageFactory);

        return source.Add(new FaultedRespondActivity<TInstance, TData, TException, TMessage>(MessageFactory<TMessage>.Create(messageFactory, callback)));
    }
}
