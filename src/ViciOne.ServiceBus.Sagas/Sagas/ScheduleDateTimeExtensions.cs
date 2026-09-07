using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for schedule date time.</summary>
public static class ScheduleDateTimeExtensions
{
    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, TMessage message, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, Task<TMessage> message, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, EventMessageFactory<TInstance, TMessage> messageFactory, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, AsyncEventMessageFactory<TInstance, TMessage> messageFactory, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, Func<BehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeProvider<TInstance> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule, TMessage message, ScheduleTimeProvider<TInstance, TData> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule, Task<TMessage> message, ScheduleTimeProvider<TInstance, TData> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory,
        ScheduleTimeProvider<TInstance, TData> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(
            new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory,
        ScheduleTimeProvider<TInstance, TData> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(
            new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule,
        Func<BehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeProvider<TInstance, TData> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(
            new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, Schedule<TInstance, TMessage> schedule, TMessage message,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, Schedule<TInstance, TMessage> schedule, Task<TMessage> message,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, Schedule<TInstance, TMessage> schedule,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, Schedule<TInstance, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TException> source, Schedule<TInstance, TMessage> schedule,
        Func<BehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, Schedule<TInstance, TMessage> schedule, TMessage message,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TData, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, Schedule<TInstance, TMessage> schedule, Task<TMessage> message,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TData, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, Schedule<TInstance, TMessage> schedule,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TData, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, Schedule<TInstance, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TData, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this ExceptionActivityBinder<TInstance, TData, TException> source, Schedule<TInstance, TMessage> schedule,
        Func<BehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TData, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }
}
