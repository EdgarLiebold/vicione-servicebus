namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Binds retry activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public class RetryActivityBinder<TInstance> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryActivities">The retry activities.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="event" />, <paramref name="retryPolicy" />, or <paramref name="retryActivities" /> is <see langword="null" />.
    /// </exception>
    public RetryActivityBinder(IEvent @event, IRetryPolicy retryPolicy, IEventActivities<TInstance> retryActivities)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(retryActivities);

        Event = @event;

        var builder = new ActivityBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in retryActivities.GetStateActivityBinders())
            activity.Bind(builder);

        IBehavior<TInstance> behavior = builder.Behavior;

        _activity = new RetryActivity<TInstance>(retryPolicy, behavior);
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


/// <summary>Binds retry activity activities to the pipeline.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class RetryActivityBinder<TInstance, TMessage> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="retryPolicy">The retry policy.</param>
    /// <param name="retryActivities">The retry activities.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="event" />, <paramref name="retryPolicy" />, or <paramref name="retryActivities" /> is <see langword="null" />.
    /// </exception>
    public RetryActivityBinder(IEvent @event, IRetryPolicy retryPolicy, IEventActivities<TInstance> retryActivities)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(retryPolicy);
        ArgumentNullException.ThrowIfNull(retryActivities);

        Event = @event;

        var builder = new ActivityBehaviorBuilder<TInstance>();

        foreach (IActivityBinder<TInstance> activity in retryActivities.GetStateActivityBinders())
            activity.Bind(builder);

        IBehavior<TInstance> behavior = builder.Behavior;

        _activity = new RetryActivity<TInstance, TMessage>(retryPolicy, behavior);
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
