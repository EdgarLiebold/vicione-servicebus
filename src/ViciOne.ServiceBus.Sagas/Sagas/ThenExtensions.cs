using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for then.</summary>
public static class ThenExtensions
{
    /// <summary>Adds a synchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The synchronous delegate.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Then<TSaga>(this IEventActivityBinder<TSaga> binder, Action<IBehaviorContext<TSaga>> action)
        where TSaga : class, ISagaStateMachineInstance
    {
        return binder.Add(new ActionActivity<TSaga>(action));
    }

    /// <summary>Adds a synchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The synchronous delegate.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> Then<TSaga, TException>(this IExceptionActivityBinder<TSaga, TException> binder,
        Action<IBehaviorExceptionContext<TSaga, TException>> action)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
    {
        return binder.Add(new FaultedActionActivity<TSaga, TException>(action));
    }

    /// <summary>Adds a asynchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="asyncAction">The asynchronous delegate.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TException> ThenAwaited<TSaga, TException>(this IExceptionActivityBinder<TSaga, TException> binder,
        Func<IBehaviorExceptionContext<TSaga, TException>, Task> asyncAction)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
    {
        return binder.Add(new AsyncFaultedActionActivity<TSaga, TException>(asyncAction));
    }

    /// <summary>Adds an asynchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The asynchronous delegate.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> ThenAwaited<TSaga>(this IEventActivityBinder<TSaga> binder, Func<IBehaviorContext<TSaga>, Task> action)
        where TSaga : class, ISagaStateMachineInstance
    {
        return binder.Add(new AsyncActivity<TSaga>(action));
    }

    /// <summary>Adds a synchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The synchronous delegate.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Then<TSaga, TData>(this IEventActivityBinder<TSaga, TData> binder,
        Action<IBehaviorContext<TSaga, TData>> action)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(action);

        return binder.Add(new ActionActivity<TSaga, TData>(action));
    }

    /// <summary>Adds a synchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The synchronous delegate.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> Then<TSaga, TData, TException>(
        this IExceptionActivityBinder<TSaga, TData, TException> binder,
        Action<IBehaviorExceptionContext<TSaga, TData, TException>> action)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TData : class
    {
        return binder.Add(new FaultedActionActivity<TSaga, TData, TException>(action));
    }

    /// <summary>Adds a asynchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="asyncAction">The asynchronous delegate.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static IExceptionActivityBinder<TSaga, TData, TException> ThenAwaited<TSaga, TData, TException>(
        this IExceptionActivityBinder<TSaga, TData, TException> binder,
        Func<IBehaviorExceptionContext<TSaga, TData, TException>, Task> asyncAction)
        where TSaga : class, ISagaStateMachineInstance
        where TException : Exception
        where TData : class
    {
        return binder.Add(new AsyncFaultedActionActivity<TSaga, TData, TException>(asyncAction));
    }

    /// <summary>Adds an asynchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The asynchronous delegate.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> ThenAwaited<TSaga, TData>(this IEventActivityBinder<TSaga, TData> binder,
        Func<IBehaviorContext<TSaga, TData>, Task> action)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(action);

        return binder.Add(new AsyncActivity<TSaga, TData>(action));
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Execute<TSaga>(this IEventActivityBinder<TSaga> binder,
        Func<IBehaviorContext<TSaga>, IStateMachineActivity<TSaga>> activityFactory)
        where TSaga : class, ISagaStateMachineInstance
    {
        var activity = new FactoryActivity<TSaga>(activityFactory);
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activity">An existing activity.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> Execute<TSaga>(this IEventActivityBinder<TSaga> binder, IStateMachineActivity<TSaga> activity)
        where TSaga : class, ISagaStateMachineInstance
    {
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga> ExecuteAwaited<TSaga>(this IEventActivityBinder<TSaga> binder,
        Func<IBehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> activityFactory)
        where TSaga : class, ISagaStateMachineInstance
    {
        var activity = new AsyncFactoryActivity<TSaga>(activityFactory);
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Execute<TSaga, TData>(this IEventActivityBinder<TSaga, TData> binder,
        Func<IBehaviorContext<TSaga, TData>, IStateMachineActivity<TSaga, TData>> activityFactory)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        var activity = new FactoryActivity<TSaga, TData>(activityFactory);
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> ExecuteAwaited<TSaga, TData>(this IEventActivityBinder<TSaga, TData> binder,
        Func<IBehaviorContext<TSaga, TData>, Task<IStateMachineActivity<TSaga, TData>>> activityFactory)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        var activity = new AsyncFactoryActivity<TSaga, TData>(activityFactory);
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> Execute<TSaga, TData>(this IEventActivityBinder<TSaga, TData> binder,
        Func<IBehaviorContext<TSaga, TData>, IStateMachineActivity<TSaga>> activityFactory)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        var activity = new FactoryActivity<TSaga, TData>(context =>
        {
            IStateMachineActivity<TSaga> newActivity = activityFactory(context);

            return new SlimActivity<TSaga, TData>(newActivity);
        });

        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TSaga, TData> ExecuteAwaited<TSaga, TData>(this IEventActivityBinder<TSaga, TData> binder,
        Func<IBehaviorContext<TSaga, TData>, Task<IStateMachineActivity<TSaga>>> activityFactory)
        where TSaga : class, ISagaStateMachineInstance
        where TData : class
    {
        var activity = new AsyncFactoryActivity<TSaga, TData>(async context =>
        {
            IStateMachineActivity<TSaga> newActivity = await activityFactory(context).ConfigureAwait(false);

            return new SlimActivity<TSaga, TData>(newActivity);
        });

        return binder.Add(activity);
    }
}
