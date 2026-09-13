using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for send by convention.</summary>
public static class SendByConventionExtensions
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Send<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        TMessage message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> SendAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        Task<TMessage> message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Send<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventMessageFactory<TInstance, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> SendAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        AsyncEventMessageFactory<TInstance, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> SendAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Send<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        TMessage message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> SendAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Task<TMessage> message, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Send<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> SendAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> SendAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Send<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> SendAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Send<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> SendAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> SendAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Send<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> SendAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Send<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> SendAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> SendAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Send<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        TMessage message, SendContextCallback<TInstance, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> SendAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        Task<TMessage> message, SendContextCallback<TInstance, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> Send<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventMessageFactory<TInstance, TMessage> messageFactory, SendContextCallback<TInstance, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> SendAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        AsyncEventMessageFactory<TInstance, TMessage> messageFactory, SendContextCallback<TInstance, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance> SendAwaited<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, SendContextCallback<TInstance, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Send<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        TMessage message, SendContextCallback<TInstance, TData, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> SendAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Task<TMessage> message, SendContextCallback<TInstance, TData, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> Send<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory, SendContextCallback<TInstance, TData, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> SendAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory, SendContextCallback<TInstance, TData, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> SendAwaited<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, SendContextCallback<TInstance, TData, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new SendActivity<TInstance, TData, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Send<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, TMessage message,
        SendExceptionContextCallback<TInstance, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> SendAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, Task<TMessage> message,
        SendExceptionContextCallback<TInstance, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Send<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TInstance, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> SendAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TInstance, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TException> SendAwaited<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendExceptionContextCallback<TInstance, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Send<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, TMessage message,
        SendExceptionContextCallback<TInstance, TData, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> SendAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, Task<TMessage> message,
        SendExceptionContextCallback<TInstance, TData, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Send<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TInstance, TData, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> SendAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        SendExceptionContextCallback<TInstance, TData, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> SendAwaited<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        SendExceptionContextCallback<TInstance, TData, TException, TMessage> callback)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedSendActivity<TInstance, TData, TException, TMessage>(context => EndpointConvention.GetDestinationAddress<TMessage>(context),
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

}
