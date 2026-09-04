using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a conditional activity binder implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class ConditionalActivityBinder<TSaga> :
    IActivityBinder<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly StateMachineAsyncCondition<TSaga> _condition;
    readonly EventActivities<TSaga> _elseActivities;
    readonly EventActivities<TSaga> _thenActivities;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenActivities">The then activities value.</param>
    /// <param name="elseActivities">The else activities value.</param>
    public ConditionalActivityBinder(Event @event, StateMachineCondition<TSaga> condition,
        EventActivities<TSaga> thenActivities, EventActivities<TSaga> elseActivities)
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
    public ConditionalActivityBinder(Event @event, StateMachineAsyncCondition<TSaga> condition,
        EventActivities<TSaga> thenActivities, EventActivities<TSaga> elseActivities)
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
    public void Bind(State<TSaga> state)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TSaga> builder)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TSaga> GetBehavior(EventActivities<TSaga> activities)
    {
        var builder = new ActivityBehaviorBuilder<TSaga>();

        foreach (IActivityBinder<TSaga> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}


/// <summary>
/// Provides a conditional activity binder implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConditionalActivityBinder<TSaga, TMessage> :
    IActivityBinder<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly StateMachineAsyncCondition<TSaga, TMessage> _condition;
    readonly EventActivities<TSaga> _elseActivities;
    readonly EventActivities<TSaga> _thenActivities;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="condition">The condition value.</param>
    /// <param name="thenActivities">The then activities value.</param>
    /// <param name="elseActivities">The else activities value.</param>
    public ConditionalActivityBinder(Event @event, StateMachineCondition<TSaga, TMessage> condition,
        EventActivities<TSaga> thenActivities, EventActivities<TSaga> elseActivities)
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
    public ConditionalActivityBinder(Event @event, StateMachineAsyncCondition<TSaga, TMessage> condition,
        EventActivities<TSaga> thenActivities, EventActivities<TSaga> elseActivities)
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
    public void Bind(State<TSaga> state)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga, TMessage>(_condition, thenBehavior, elseBehavior);

        state.Bind(Event, conditionActivity);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TSaga> builder)
    {
        IBehavior<TSaga> thenBehavior = GetBehavior(_thenActivities);
        IBehavior<TSaga> elseBehavior = GetBehavior(_elseActivities);

        var conditionActivity = new ConditionActivity<TSaga, TMessage>(_condition, thenBehavior, elseBehavior);

        builder.Add(conditionActivity);
    }

    static IBehavior<TSaga> GetBehavior(EventActivities<TSaga> activities)
    {
        var builder = new ActivityBehaviorBuilder<TSaga>();

        foreach (IActivityBinder<TSaga> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        return builder.Behavior;
    }
}
