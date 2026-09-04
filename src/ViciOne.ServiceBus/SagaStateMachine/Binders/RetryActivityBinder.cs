namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a retry activity binder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public class RetryActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryActivities">The retry activities value.</param>
    public RetryActivityBinder(Event @event, IRetryPolicy retryPolicy, EventActivities<TInstance> retryActivities)
    {
        Event = @event;

        var builder = new ActivityBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in retryActivities.GetStateActivityBinders())
            activity.Bind(builder);

        IBehavior<TInstance> behavior = builder.Behavior;

        _activity = new RetryActivity<TInstance>(retryPolicy, behavior);
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


/// <summary>
/// Provides a retry activity binder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RetryActivityBinder<TInstance, TMessage> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="retryActivities">The retry activities value.</param>
    public RetryActivityBinder(Event @event, IRetryPolicy retryPolicy, EventActivities<TInstance> retryActivities)
    {
        Event = @event;

        var builder = new ActivityBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in retryActivities.GetStateActivityBinders())
            activity.Bind(builder);

        IBehavior<TInstance> behavior = builder.Behavior;

        _activity = new RetryActivity<TInstance, TMessage>(retryPolicy, behavior);
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
