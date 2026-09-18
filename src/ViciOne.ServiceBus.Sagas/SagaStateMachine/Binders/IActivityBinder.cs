namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Defines the operations required by activity binder.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IActivityBinder<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the event.</summary>
    IEvent Event { get; }

    /// <summary>
    /// Returns True if the event is a state transition event (enter/leave/afterLeave/beforeEnter)
    /// for the specified state.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    bool IsStateTransitionEvent(IState state);

    /// <summary>Binds the activity to the state, may also just ignore the event if it's an ignore event.</summary>
    /// <param name="state">The state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    void Bind(IState<TSaga> state);

    /// <summary>Bind the activities to the builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    void Bind(IBehaviorBuilder<TSaga> builder);
}
