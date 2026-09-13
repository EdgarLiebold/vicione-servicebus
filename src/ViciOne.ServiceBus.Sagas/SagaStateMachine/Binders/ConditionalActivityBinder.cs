using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds conditional activity activities to the pipeline.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ConditionalActivityBinder<TSaga> :
    IActivityBinder<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly StateMachineAsyncCondition<TSaga> _condition;
    readonly IEventActivities<TSaga> _elseActivities;
    readonly IEventActivities<TSaga> _thenActivities;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalActivityBinder(IEvent @event, StateMachineCondition<TSaga> condition,
        IEventActivities<TSaga> thenActivities, IEventActivities<TSaga> elseActivities)
        : this(@event, context => Task.FromResult(condition(context)), thenActivities, elseActivities)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalActivityBinder(IEvent @event, StateMachineAsyncCondition<TSaga> condition,
        IEventActivities<TSaga> thenActivities, IEventActivities<TSaga> elseActivities)
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
    public void Bind(IState<TSaga> state)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TSaga> builder)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TSaga> GetBehavior(IEventActivities<TSaga> activities)
    {
        var builder = new ActivityBehaviorBuilder<TSaga>();

        foreach (IActivityBinder<TSaga> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}


/// <summary>Binds conditional activity activities to the pipeline.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConditionalActivityBinder<TSaga, TMessage> :
    IActivityBinder<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly StateMachineAsyncCondition<TSaga, TMessage> _condition;
    readonly IEventActivities<TSaga> _elseActivities;
    readonly IEventActivities<TSaga> _thenActivities;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalActivityBinder(IEvent @event, StateMachineCondition<TSaga, TMessage> condition,
        IEventActivities<TSaga> thenActivities, IEventActivities<TSaga> elseActivities)
        : this(@event, context => Task.FromResult(condition(context)), thenActivities, elseActivities)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="condition">The condition.</param>
    /// <param name="thenActivities">The then activities.</param>
    /// <param name="elseActivities">The else activities.</param>
    public ConditionalActivityBinder(IEvent @event, StateMachineAsyncCondition<TSaga, TMessage> condition,
        IEventActivities<TSaga> thenActivities, IEventActivities<TSaga> elseActivities)
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
    public void Bind(IState<TSaga> state)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga, TMessage>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TSaga> builder)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga, TMessage>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TSaga> GetBehavior(IEventActivities<TSaga> activities)
    {
        var builder = new ActivityBehaviorBuilder<TSaga>();

        foreach (IActivityBinder<TSaga> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}
