namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Routes event activities to an activities.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class ExecuteActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="activity">The activity.</param>
    public ExecuteActivityBinder(Event @event, IStateMachineActivity<TInstance> activity)
    {
        Event = @event;
        _activity = activity;
    }

    /// <summary>Gets the event.</summary>
    public Event Event { get; }

    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(State state)
    {
        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    public void Bind(State<TInstance> state)
    {
        state.Bind(Event, _activity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        builder.Add(_activity);
    }
}
