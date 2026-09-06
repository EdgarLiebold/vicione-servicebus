using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state observer.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IStateObserver<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Observes a state-machine transition after the current state has been updated.
    /// </summary>
    /// <param name="context">The instance context of the state machine</param>
    /// <param name="currentState">The current state (after the change)</param>
    /// <param name="previousState">The previous state (before the change)</param>
    /// <returns>A task that completes when the observer finishes processing the transition.</returns>
    Task StateChangedAsync(BehaviorContext<TSaga> context, State currentState, State? previousState);
}
