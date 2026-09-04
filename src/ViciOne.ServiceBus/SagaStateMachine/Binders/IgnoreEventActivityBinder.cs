namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an ignore event activity binder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public class IgnoreEventActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    public IgnoreEventActivityBinder(Event @event)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
    }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    public Event Event { get; } = null!;
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
        state.Ignore(Event);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
    }
}


/// <summary>
/// Provides an ignore event activity binder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
public class IgnoreEventActivityBinder<TInstance, TData> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
{
    readonly Event<TData> _event = null!;
    readonly StateMachineCondition<TInstance, TData> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="filter">The filter value.</param>
    public IgnoreEventActivityBinder(Event<TData> @event, StateMachineCondition<TInstance, TData> filter)
    {
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    public Event Event => _event;
    /// <summary>
    /// Determines whether state transition event.
    /// </summary>
    /// <param name="state">The state value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(State state)
    {
        return Equals(_event, state.Enter) || Equals(_event, state.BeforeEnter)
            || Equals(_event, state.AfterLeave) || Equals(_event, state.Leave);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="state">The state value.</param>
    public void Bind(State<TInstance> state)
    {
        state.Ignore(_event, _filter);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
    }
}
