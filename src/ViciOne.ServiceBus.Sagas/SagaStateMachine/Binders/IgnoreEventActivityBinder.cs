namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds ignore event activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class IgnoreEventActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    public IgnoreEventActivityBinder(Event @event)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
    }

    /// <summary>Gets the event.</summary>
    public Event Event { get; } = null!;
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
        state.Ignore(Event);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
    }
}


/// <summary>Binds ignore event activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public class IgnoreEventActivityBinder<TInstance, TData> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
{
    readonly Event<TData> _event = null!;
    readonly StateMachineCondition<TInstance, TData> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public IgnoreEventActivityBinder(Event<TData> @event, StateMachineCondition<TInstance, TData> filter)
    {
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>Gets the event.</summary>
    public Event Event => _event;
    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(State state)
    {
        return Equals(_event, state.Enter) || Equals(_event, state.BeforeEnter)
            || Equals(_event, state.AfterLeave) || Equals(_event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    public void Bind(State<TInstance> state)
    {
        state.Ignore(_event, _filter);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
    }
}
