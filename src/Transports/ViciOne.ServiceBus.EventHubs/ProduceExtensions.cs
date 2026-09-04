using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubs.Activities;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides extension methods for produce.
/// </summary>
public static class ProduceExtensions
{
    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Produce<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, TMessage message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Produce<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Produce<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, AsyncEventMessageFactory<TInstance, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Produce<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, Func<BehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, TMessage message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, Func<BehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider, TMessage message,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider,
        Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider,
        Func<BehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        TMessage message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="nameProvider">The name provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="contextCallback">The context callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        Func<BehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }
}
