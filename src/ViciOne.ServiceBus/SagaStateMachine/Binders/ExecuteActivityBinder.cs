namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Routes event activities to an activities
/// </summary>
/// <typeparam name="TInstance"></typeparam>
public class ExecuteActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="activity">The activity value.</param>
    public ExecuteActivityBinder(Event @event, IStateMachineActivity<TInstance> activity)
    {
        Event = @event;
        _activity = activity;
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
        state.Bind(Event, _activity);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        builder.Add(_activity);
    }
}
