namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Defines the operations required by activity binder.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IActivityBinder<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Gets the event.</summary>
    Event Event { get; }

    /// <summary>
    /// Returns True if the event is a state transition event (enter/leave/afterLeave/beforeEnter)
    /// for the specified state.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsStateTransitionEvent(State state);

    /// <summary>Binds the activity to the state, may also just ignore the event if it's an ignore event.</summary>
    /// <param name="state">The state.</param>
    void Bind(State<TSaga> state);

    /// <summary>Bind the activities to the builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Bind(IBehaviorBuilder<TSaga> builder);
}
