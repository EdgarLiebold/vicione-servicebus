namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Routes event activities to an activities.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class ExecuteActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="activity">The activity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event" /> or <paramref name="activity" /> is <see langword="null" />.</exception>
    public ExecuteActivityBinder(IEvent @event, IStateMachineActivity<TInstance> activity)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(activity);

        Event = @event;
        _activity = activity;
    }

    /// <summary>Gets the event.</summary>
    public IEvent Event { get; }

    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    public bool IsStateTransitionEvent(IState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    public void Bind(IState<TInstance> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.Bind(Event, _activity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(_activity);
    }
}
