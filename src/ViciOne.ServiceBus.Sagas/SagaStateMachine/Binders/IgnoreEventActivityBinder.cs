namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds ignore event activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class IgnoreEventActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event" /> is <see langword="null" />.</exception>
    public IgnoreEventActivityBinder(IEvent @event)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
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

        state.Ignore(Event);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
    }
}


/// <summary>Binds ignore event activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public class IgnoreEventActivityBinder<TInstance, TData> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
{
    readonly IEvent<TData> _event;
    readonly StateMachineCondition<TInstance, TData> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event" /> or <paramref name="filter" /> is <see langword="null" />.</exception>
    public IgnoreEventActivityBinder(IEvent<TData> @event, StateMachineCondition<TInstance, TData> filter)
    {
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>Gets the event.</summary>
    public IEvent Event => _event;

    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    public bool IsStateTransitionEvent(IState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return Equals(_event, state.Enter) || Equals(_event, state.BeforeEnter)
            || Equals(_event, state.AfterLeave) || Equals(_event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    public void Bind(IState<TInstance> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        state.Ignore(_event, _filter);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
    }
}
