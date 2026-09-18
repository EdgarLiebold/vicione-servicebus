using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Creates a compensation activity with the compensation behavior.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class CatchActivityBinder<TInstance, TException> :
    IActivityBinder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
    where TException : Exception
{
    readonly IStateMachineActivity<TInstance> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="activities">The activities.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event" /> or <paramref name="activities" /> is <see langword="null" />.</exception>
    public CatchActivityBinder(IEvent @event, IEventActivities<TInstance> activities)
    {
        ArgumentNullException.ThrowIfNull(@event, nameof(@event));
        ArgumentNullException.ThrowIfNull(activities);

        Event = @event;

        var builder = new CatchBehaviorBuilder<TInstance>();
        foreach (IActivityBinder<TInstance> activity in activities.GetStateActivityBinders())
            activity.Bind(builder);

        _activity = new CatchFaultActivity<TInstance, TException>(builder.Behavior);
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
