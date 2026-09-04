using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Creates a compensation activity with the compensation behavior
/// </summary>
/// <typeparam name="TInstance"></typeparam>
/// <typeparam name="TException"></typeparam>
public class CatchActivityBinder<TInstance, TException> :
    IActivityBinder<TInstance>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
{
    readonly EventActivities<TInstance> _activities;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="activities">The activities value.</param>
    public CatchActivityBinder(Event @event, EventActivities<TInstance> activities)
    {
        Event = @event;
        _activities = activities;
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
        var builder = new CatchBehaviorBuilder<TInstance>();
        foreach (IActivityBinder<TInstance> activity in _activities.GetStateActivityBinders())
            activity.Bind(builder);

        var compensateActivity = new CatchFaultActivity<TInstance, TException>(builder.Behavior);

        state.Bind(Event, compensateActivity);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Bind(IBehaviorBuilder<TInstance> builder)
    {
        var compensateActivityBuilder = new CatchBehaviorBuilder<TInstance>();
        foreach (IActivityBinder<TInstance> activity in _activities.GetStateActivityBinders())
            activity.Bind(compensateActivityBuilder);

        var compensateActivity = new CatchFaultActivity<TInstance, TException>(compensateActivityBuilder.Behavior);

        builder.Add(compensateActivity);
    }
}
