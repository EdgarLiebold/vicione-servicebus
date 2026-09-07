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
    public static EventActivityBinder<TSaga> Then<TSaga>(this EventActivityBinder<TSaga> binder, Action<BehaviorContext<TSaga>> action)
        where TSaga : class, SagaStateMachineInstance
    {
        return binder.Add(new ActionActivity<TSaga>(action));
    }

    /// <summary>Adds a synchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The synchronous delegate.</param>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public static ExceptionActivityBinder<TSaga, TException> Then<TSaga, TException>(this ExceptionActivityBinder<TSaga, TException> binder,
        Action<BehaviorExceptionContext<TSaga, TException>> action)
        where TSaga : class, SagaStateMachineInstance
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
    public static ExceptionActivityBinder<TSaga, TException> ThenAwaited<TSaga, TException>(this ExceptionActivityBinder<TSaga, TException> binder,
        Func<BehaviorExceptionContext<TSaga, TException>, Task> asyncAction)
        where TSaga : class, SagaStateMachineInstance
        where TException : Exception
    {
        return binder.Add(new AsyncFaultedActionActivity<TSaga, TException>(asyncAction));
    }

    /// <summary>Adds an asynchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The asynchronous delegate.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> ThenAwaited<TSaga>(this EventActivityBinder<TSaga> binder, Func<BehaviorContext<TSaga>, Task> action)
        where TSaga : class, SagaStateMachineInstance
    {
        return binder.Add(new AsyncActivity<TSaga>(action));
    }

    /// <summary>Adds a synchronous delegate activity to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <typeparam name="TData">The event data type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="action">The synchronous delegate.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga, TData> Then<TSaga, TData>(this EventActivityBinder<TSaga, TData> binder,
        Action<BehaviorContext<TSaga, TData>> action)
        where TSaga : class, SagaStateMachineInstance
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
    public static ExceptionActivityBinder<TSaga, TData, TException> Then<TSaga, TData, TException>(
        this ExceptionActivityBinder<TSaga, TData, TException> binder,
        Action<BehaviorExceptionContext<TSaga, TData, TException>> action)
        where TSaga : class, SagaStateMachineInstance
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
    public static ExceptionActivityBinder<TSaga, TData, TException> ThenAwaited<TSaga, TData, TException>(
        this ExceptionActivityBinder<TSaga, TData, TException> binder,
        Func<BehaviorExceptionContext<TSaga, TData, TException>, Task> asyncAction)
        where TSaga : class, SagaStateMachineInstance
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
    public static EventActivityBinder<TSaga, TData> ThenAwaited<TSaga, TData>(this EventActivityBinder<TSaga, TData> binder,
        Func<BehaviorContext<TSaga, TData>, Task> action)
        where TSaga : class, SagaStateMachineInstance
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
    public static EventActivityBinder<TSaga> Execute<TSaga>(this EventActivityBinder<TSaga> binder,
        Func<BehaviorContext<TSaga>, IStateMachineActivity<TSaga>> activityFactory)
        where TSaga : class, SagaStateMachineInstance
    {
        var activity = new FactoryActivity<TSaga>(activityFactory);
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activity">An existing activity.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> Execute<TSaga>(this EventActivityBinder<TSaga> binder, IStateMachineActivity<TSaga> activity)
        where TSaga : class, SagaStateMachineInstance
    {
        return binder.Add(activity);
    }

    /// <summary>Add an activity execution to the event's behavior.</summary>
    /// <typeparam name="TSaga">The state machine instance type.</typeparam>
    /// <param name="binder">The event binder.</param>
    /// <param name="activityFactory">The factory method which returns the activity to execute.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<TSaga> ExecuteAwaited<TSaga>(this EventActivityBinder<TSaga> binder,
        Func<BehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> activityFactory)
        where TSaga : class, SagaStateMachineInstance
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
    public static EventActivityBinder<TSaga, TData> Execute<TSaga, TData>(this EventActivityBinder<TSaga, TData> binder,
        Func<BehaviorContext<TSaga, TData>, IStateMachineActivity<TSaga, TData>> activityFactory)
        where TSaga : class, SagaStateMachineInstance
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
    public static EventActivityBinder<TSaga, TData> ExecuteAwaited<TSaga, TData>(this EventActivityBinder<TSaga, TData> binder,
        Func<BehaviorContext<TSaga, TData>, Task<IStateMachineActivity<TSaga, TData>>> activityFactory)
        where TSaga : class, SagaStateMachineInstance
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
    public static EventActivityBinder<TSaga, TData> Execute<TSaga, TData>(this EventActivityBinder<TSaga, TData> binder,
        Func<BehaviorContext<TSaga, TData>, IStateMachineActivity<TSaga>> activityFactory)
        where TSaga : class, SagaStateMachineInstance
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
    public static EventActivityBinder<TSaga, TData> ExecuteAwaited<TSaga, TData>(this EventActivityBinder<TSaga, TData> binder,
        Func<BehaviorContext<TSaga, TData>, Task<IStateMachineActivity<TSaga>>> activityFactory)
        where TSaga : class, SagaStateMachineInstance
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
