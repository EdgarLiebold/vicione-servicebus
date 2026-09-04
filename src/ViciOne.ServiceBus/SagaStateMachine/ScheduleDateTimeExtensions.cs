using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides extension methods for schedule date time.
/// </summary>
public static class ScheduleDateTimeExtensions
{
    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, TMessage message, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, Task<TMessage> message, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, EventMessageFactory<TInstance, TMessage> messageFactory, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, AsyncEventMessageFactory<TInstance, TMessage> messageFactory, ScheduleTimeProvider<TInstance> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance> Schedule<TInstance, TMessage>(this EventActivityBinder<TInstance> source,
        Schedule<TInstance, TMessage> schedule, Func<BehaviorContext<TInstance>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleTimeProvider<TInstance> timeProvider, Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule, TMessage message, ScheduleTimeProvider<TInstance, TData> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
    public static EventActivityBinder<TInstance, TData> Schedule<TInstance, TData, TMessage>(this EventActivityBinder<TInstance, TData> source,
        Schedule<TInstance, TMessage> schedule, Task<TMessage> message, ScheduleTimeProvider<TInstance, TData> timeProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TInstance : class, SagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        return source.Add(new ScheduleActivity<TInstance, TData, TMessage>(schedule, timeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the schedule operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="callback">The callback value.</param>
    /// <returns>The result of the operation.</returns>
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
