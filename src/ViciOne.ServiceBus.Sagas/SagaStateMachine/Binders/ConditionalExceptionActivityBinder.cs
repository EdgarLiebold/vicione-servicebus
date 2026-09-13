using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds conditional exception activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class ConditionalExceptionActivityBinder<TInstance, TException> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
    where TException : Exception
{
    readonly StateMachineAsyncExceptionCondition<TInstance, TException> _condition;
    readonly IEventActivities<TInstance> _elseActivities;
    readonly IEventActivities<TInstance> _thenActivities;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalExceptionActivityBinder(IEvent @event, StateMachineExceptionCondition<TInstance, TException> condition,
        IEventActivities<TInstance> thenActivities, IEventActivities<TInstance> elseActivities)
        : this(@event, context => Task.FromResult(condition(context)), thenActivities, elseActivities)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalExceptionActivityBinder(IEvent @event, StateMachineAsyncExceptionCondition<TInstance, TException> condition,
        IEventActivities<TInstance> thenActivities, IEventActivities<TInstance> elseActivities)
    {
        _thenActivities = thenActivities;
        _elseActivities = elseActivities;
        _condition = condition;
        Event = @event;
    }

    /// <summary>Gets the event.</summary>
    public IEvent Event { get; }

    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(IState state)
    {
        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    public void Bind(IState<TInstance> state)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TException>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TException>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TInstance> GetBehavior(IEventActivities<TInstance> activities)
    {
        var builder = new CatchBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}


/// <summary>Binds conditional exception activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class ConditionalExceptionActivityBinder<TInstance, TData, TException> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
    where TException : Exception
    where TData : class
{
    readonly StateMachineAsyncExceptionCondition<TInstance, TData, TException> _condition;
    readonly IEventActivities<TInstance> _elseActivities;
    readonly IEventActivities<TInstance> _thenActivities;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalExceptionActivityBinder(IEvent @event, StateMachineExceptionCondition<TInstance, TData, TException> condition,
        IEventActivities<TInstance> thenActivities, IEventActivities<TInstance> elseActivities)
        : this(@event, context => Task.FromResult(condition(context)), thenActivities, elseActivities)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalExceptionActivityBinder(IEvent @event, StateMachineAsyncExceptionCondition<TInstance, TData, TException> condition,
        IEventActivities<TInstance> thenActivities, IEventActivities<TInstance> elseActivities)
    {
        _thenActivities = thenActivities;
        _elseActivities = elseActivities;
        _condition = condition;
        Event = @event;
    }

    /// <summary>Gets the event.</summary>
    public IEvent Event { get; }

    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(IState state)
    {
        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    public void Bind(IState<TInstance> state)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TData, TException>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        IBehavior<TInstance> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TInstance> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionExceptionActivity<TInstance, TData, TException>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TInstance> GetBehavior(IEventActivities<TInstance> activities)
    {
        var builder = new CatchBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}
