using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for schedule time span.</summary>
public static class ScheduleTimeSpanExtensions
{
    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, TMessage message, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, Task<TMessage> message, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, TMessage message, ScheduleDelayProvider<TSaga> delayProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, Task<TMessage> message, ScheduleDelayProvider<TSaga> delayProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, EventMessageFactory<TSaga, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, AsyncEventMessageFactory<TSaga, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, Func<IBehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, EventMessageFactory<TSaga, TMessage> messageFactory, ScheduleDelayProvider<TSaga> delayProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, AsyncEventMessageFactory<TSaga, TMessage> messageFactory,
        ScheduleDelayProvider<TSaga> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Schedule<TSaga, TMessage>(this IEventActivityBinder<TSaga> source,
        ISchedule<TSaga, TMessage> schedule, Func<IBehaviorContext<TSaga>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleDelayProvider<TSaga> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, TMessage message, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, Task<TMessage> message, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, TMessage message, ScheduleDelayProvider<TSaga, TData> delayProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, Task<TMessage> message, ScheduleDelayProvider<TSaga, TData> delayProvider,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, EventMessageFactory<TSaga, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, AsyncEventMessageFactory<TSaga, TData, TMessage> messageFactory,
        Action<SendContext<TMessage>>? callback =
            null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule, Func<IBehaviorContext<TSaga, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        Action<SendContext<TMessage>>? callback =
            null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule,
        EventMessageFactory<TSaga, TData, TMessage> messageFactory,
        ScheduleDelayProvider<TSaga, TData> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule,
        AsyncEventMessageFactory<TSaga, TData, TMessage> messageFactory,
        ScheduleDelayProvider<TSaga, TData> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Schedule<TSaga, TData, TMessage>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga, TMessage> schedule,
        Func<IBehaviorContext<TSaga, TData>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleDelayProvider<TSaga, TData> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorContext<TSaga, TData> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new ScheduleActivity<TSaga, TData, TMessage>(schedule, TimeProvider, MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule, TMessage message,
        ScheduleDelayExceptionProvider<TSaga, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule, Task<TMessage> message,
        ScheduleDelayExceptionProvider<TSaga, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule,
        EventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule,
        Func<IBehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule,
        EventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory,
        ScheduleDelayExceptionProvider<TSaga, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TSaga, TException, TMessage> messageFactory,
        ScheduleDelayExceptionProvider<TSaga, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Schedule<TSaga, TException, TMessage>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga, TMessage> schedule,
        Func<IBehaviorExceptionContext<TSaga, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleDelayExceptionProvider<TSaga, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule, TMessage message,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule, Task<TMessage> message,
        Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule, TMessage message,
        ScheduleDelayExceptionProvider<TSaga, TData, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule, Task<TMessage> message,
        ScheduleDelayExceptionProvider<TSaga, TData, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(message, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule,
        EventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule,
        Func<IBehaviorExceptionContext<TSaga, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + schedule.GetDelay(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule,
        EventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory,
        ScheduleDelayExceptionProvider<TSaga, TData, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule,
        AsyncEventExceptionMessageFactory<TSaga, TData, TException, TMessage> messageFactory,
        ScheduleDelayExceptionProvider<TSaga, TData, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Schedules the supplied message or operation.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <param name="delayProvider">The delay provider.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Schedule<TSaga, TData, TException, TMessage>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga, TMessage> schedule,
        Func<IBehaviorExceptionContext<TSaga, TData, TException>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>> messageFactory,
        ScheduleDelayExceptionProvider<TSaga, TData, TException> delayProvider, Action<SendContext<TMessage>>? callback = null)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
        where TMessage : class
    {
        DateTimeOffset TimeProvider(IBehaviorExceptionContext<TSaga, TData, TException> context)
        {
            return context.GetTimeProvider().GetUtcNow() + delayProvider(context);
        }

        return source.Add(new FaultedScheduleActivity<TSaga, TData, TException, TMessage>(schedule, TimeProvider,
            MessageFactory<TMessage>.Create(messageFactory, callback)));
    }

    /// <summary>Unschedule a message, if the message was scheduled.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Unschedule<TSaga, TData>(this IEventActivityBinder<TSaga, TData> source,
        ISchedule<TSaga> schedule)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        return source.Add(new UnscheduleActivity<TSaga>(schedule));
    }

    /// <summary>Unschedule a message, if the message was scheduled.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Unschedule<TSaga, TData, TException>(
        this IExceptionActivityBinder<TSaga, TData, TException> source, ISchedule<TSaga> schedule)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
        where TException : Exception
    {
        return source.Add(new FaultedUnscheduleActivity<TSaga>(schedule));
    }

    /// <summary>Unschedule a message, if the message was scheduled.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Unschedule<TSaga>(this IEventActivityBinder<TSaga> source, ISchedule<TSaga> schedule)
        where TSaga : class, ISagaStateMachineInstance
    {
        return source.Add(new UnscheduleActivity<TSaga>(schedule));
    }

    /// <summary>Unschedule a message, if the message was scheduled.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="schedule">The schedule.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Unschedule<TSaga, TException>(this IExceptionActivityBinder<TSaga, TException> source,
        ISchedule<TSaga> schedule)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
    {
        return source.Add(new FaultedUnscheduleActivity<TSaga>(schedule));
    }
}
