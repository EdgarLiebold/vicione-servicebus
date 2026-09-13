using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubs.Activities;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Adds Event Hubs produce activities to saga state-machine behavior binders.</summary>
public static class ProduceExtensions
{
    /// <summary>Adds an activity that produces a fixed message when the behavior executes.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="message">The message to produce.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance> Produce<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, TMessage message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an activity that awaits and produces a message when the behavior executes.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="message">The task that supplies the message.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance> Produce<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an activity that asynchronously creates and produces a message from the behavior context.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="messageFactory">Creates the message from the current behavior context.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance> Produce<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, AsyncEventMessageFactory<TInstance, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an activity that asynchronously initializes and produces a message from the behavior context.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="messageFactory">Creates the initialized message and its initializer pipe.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance> Produce<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        EventHubNameProvider<TInstance> nameProvider, Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an activity that produces a fixed message from a data-bearing behavior.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="message">The message to produce.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, TMessage message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an activity that awaits and produces a message from a data-bearing behavior.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="message">The task that supplies the message.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an activity that asynchronously creates and produces a message from a data-bearing behavior.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="messageFactory">Creates the message from the current behavior context and data.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an activity that asynchronously initializes and produces a message from a data-bearing behavior.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the behavior context.</param>
    /// <param name="messageFactory">Creates the initialized message and its initializer pipe.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the produce activity appended.</returns>
    public static IEventActivityBinder<TInstance, TData> Produce<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        EventHubNameProvider<TInstance, TData> nameProvider, Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ProduceActivity<TInstance, TData, TMessage>(nameProvider, MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an exception activity that produces a fixed message.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="message">The message to produce.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider, TMessage message,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an exception activity that awaits and produces a message.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="message">The task that supplies the message.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider,
        Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an exception activity that asynchronously creates and produces a message.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="messageFactory">Creates the message from the current exception context.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an exception activity that asynchronously initializes and produces a message.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="messageFactory">Creates the initialized message and its initializer pipe.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TException> Produce<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ExceptionEventHubNameProvider<TInstance, TException> nameProvider,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an exception activity that produces a fixed message from a data-bearing exception context.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="message">The message to produce.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        TMessage message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an exception activity that awaits and produces a message from a data-bearing exception context.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="message">The task that supplies the message.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        Task<TMessage> message, Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(message, contextCallback)));
    }

    /// <summary>Adds an exception activity that asynchronously creates and produces a message from a data-bearing exception context.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="messageFactory">Creates the message from the current exception context and data.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }

    /// <summary>Adds an exception activity that asynchronously initializes and produces a message from a data-bearing exception context.</summary>
    /// <typeparam name="TInstance">The saga instance type.</typeparam>
    /// <typeparam name="TData">The behavior data type.</typeparam>
    /// <typeparam name="TException">The handled exception type.</typeparam>
    /// <typeparam name="TMessage">The produced message type.</typeparam>
    /// <param name="source">The exception behavior binder to extend.</param>
    /// <param name="nameProvider">Selects the destination Event Hub from the exception context.</param>
    /// <param name="messageFactory">Creates the initialized message and its initializer pipe.</param>
    /// <param name="contextCallback">Optionally configures the outbound send context.</param>
    /// <returns>The same binder with the faulted produce activity appended.</returns>
    public static IExceptionActivityBinder<TInstance, TData, TException> Produce<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ExceptionEventHubNameProvider<TInstance, TData, TException> nameProvider,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? contextCallback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
        where TException : Exception
    {
        return source.Add(new FaultedProduceActivity<TInstance, TData, TException, TMessage>(nameProvider,
            MessageFactory<TMessage>.Create(messageFactory, contextCallback)));
    }
}
