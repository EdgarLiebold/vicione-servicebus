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
    public static IEventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        ISchedule<TInstance, TMessage> schedule, TMessage message, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        ISchedule<TInstance, TMessage> schedule, Task<TMessage> message, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        ISchedule<TInstance, TMessage> schedule, EventMessageFactory<TInstance, TMessage> messageFactory, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        ISchedule<TInstance, TMessage> schedule, AsyncEventMessageFactory<TInstance, TMessage> messageFactory, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this IEventActivityBinder<TInstance> source,
        ISchedule<TInstance, TMessage> schedule, Func<IBehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeProvider<TInstance> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        ISchedule<TInstance, TMessage> schedule, TMessage message, ScheduleTimeProvider<TInstance, TData> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        ISchedule<TInstance, TMessage> schedule, Task<TMessage> message, ScheduleTimeProvider<TInstance, TData> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        ISchedule<TInstance, TMessage> schedule,
        EventMessageFactory<TInstance, TData, TMessage> messageFactory,
        ScheduleTimeProvider<TInstance, TData> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        ISchedule<TInstance, TMessage> schedule,
        AsyncEventMessageFactory<TInstance, TData, TMessage> messageFactory,
        ScheduleTimeProvider<TInstance, TData> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IEventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this IEventActivityBinder<TInstance, TData> source,
        ISchedule<TInstance, TMessage> schedule,
        Func<IBehaviorContext<TInstance, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeProvider<TInstance, TData> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ISchedule<TInstance, TMessage> schedule, TMessage message,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ISchedule<TInstance, TMessage> schedule, Task<TMessage> message,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ISchedule<TInstance, TMessage> schedule,
        EventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ISchedule<TInstance, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TInstance, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TException> Schedule<TInstance, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TException> source, ISchedule<TInstance, TMessage> schedule,
        Func<IBehaviorExceptionContext<TInstance, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ISchedule<TInstance, TMessage> schedule, TMessage message,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ISchedule<TInstance, TMessage> schedule, Task<TMessage> message,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ISchedule<TInstance, TMessage> schedule,
        EventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ISchedule<TInstance, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TInstance, TData, TException, TMessage> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
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
    public static IExceptionActivityBinder<TInstance, TData, TException> Schedule<TInstance, TData, TException, TMessage>(
        this IExceptionActivityBinder<TInstance, TData, TException> source, ISchedule<TInstance, TMessage> schedule,
        Func<IBehaviorExceptionContext<TInstance, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeExceptionProvider<TInstance, TData, TException> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        return source.Add(new FaultedScheduleActivity<TInstance, TData, TException, TMessage>(schedule, timeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }
}
