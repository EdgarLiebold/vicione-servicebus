using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a conditional exception activity binder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class ConditionalExceptionActivityBinder<TInstance, TException> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TInstance, TException> _condition;
    readonly EventActivities<TInstance> _elseActivities;
    readonly EventActivities<TInstance> _thenActivities;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenActivities">The then activities value.</param>
    /// <param name="elseActivities">The else activities value.</param>
    public ConditionalExceptionActivityBinder(Event @event, StateMachineExceptionCondition<TInstance, TException> condition,
        EventActivities<TInstance> thenActivities, EventActivities<TInstance> elseActivities)
        : this(@event, context => Task.FromResult(condition(context)), thenActivities, elseActivities)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenActivities">The then activities value.</param>
    /// <param name="elseActivities">The else activities value.</param>
    public ConditionalExceptionActivityBinder(Event @event, StateMachineAsyncExceptionCondition<TInstance, TException> condition,
        EventActivities<TInstance> thenActivities, EventActivities<TInstance> elseActivities)
    {
        _thenActivities = thenActivities;
        _elseActivities = elseActivities;
        _condition = condition;
        Event = @event;
    }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    public Event Event { get; }

    /// <summary>
    /// Determines whether state transition event.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(State state)
    {
        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    public void Bind(State<TInstance> state)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TException>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TException>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TInstance> GetBehavior(EventActivities<TInstance> activities)
    {
        var builder = new CatchBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}


/// <summary>
/// Provides a conditional exception activity binder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class ConditionalExceptionActivityBinder<TInstance, TData, TException> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
    where TData : class
{
    readonly StateMachineAsyncExceptionCondition<TInstance, TData, TException> _condition;
    readonly EventActivities<TInstance> _elseActivities;
    readonly EventActivities<TInstance> _thenActivities;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenActivities">The then activities value.</param>
    /// <param name="elseActivities">The else activities value.</param>
    public ConditionalExceptionActivityBinder(Event @event, StateMachineExceptionCondition<TInstance, TData, TException> condition,
        EventActivities<TInstance> thenActivities, EventActivities<TInstance> elseActivities)
        : this(@event, context => Task.FromResult(condition(context)), thenActivities, elseActivities)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenActivities">The then activities value.</param>
    /// <param name="elseActivities">The else activities value.</param>
    public ConditionalExceptionActivityBinder(Event @event, StateMachineAsyncExceptionCondition<TInstance, TData, TException> condition,
        EventActivities<TInstance> thenActivities, EventActivities<TInstance> elseActivities)
    {
        _thenActivities = thenActivities;
        _elseActivities = elseActivities;
        _condition = condition;
        Event = @event;
    }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    public Event Event { get; }

    /// <summary>
    /// Determines whether state transition event.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(State state)
    {
        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    public void Bind(State<TInstance> state)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TData, TException>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TData, TException>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TInstance> GetBehavior(EventActivities<TInstance> activities)
    {
        var builder = new CatchBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}
