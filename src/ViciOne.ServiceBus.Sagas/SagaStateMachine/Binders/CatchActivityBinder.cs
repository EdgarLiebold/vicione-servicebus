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
    readonly IEventActivities<TInstance> _activities;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="event">The event.</param>
    /// <param name="activities">The activities.</param>
    public CatchActivityBinder(IEvent @event, IEventActivities<TInstance> activities)
    {
        Event = @event;
        _activities = activities;
    }

    /// <summary>Gets the event.</summary>
    public IEvent Event { get; }

    /// <summary>Determines whether state transition event.</summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStateTransitionEvent(IState state)
    {
        return Equals(Event, state.Enter) || Equals(Event, state.BeforeEnter)
            || Equals(Event, state.AfterLeave) || Equals(Event, state.Leave);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="state">The state.</param>
    public void Bind(IState<TInstance> state)
    {
        var builder = new CatchBehaviorBuilder<TInstance>();
        foreach (IActivityBinder<TInstance> activity in _activities.GetStateActivityBinders())
            activity.Bind(builder);

        var compensateActivity = new CatchFaultActivity<TInstance, TException>(builder.Behavior);

        state.Bind(Event, compensateActivity);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        var compensateActivityBuilder = new CatchBehaviorBuilder<TInstance>();
        foreach (IActivityBinder<TInstance> activity in _activities.GetStateActivityBinders())
            activity.Bind(compensateActivityBuilder);

        var compensateActivity = new CatchFaultActivity<TInstance, TException>(compensateActivityBuilder.Behavior);

        builder.Add(compensateActivity);
    }
}
