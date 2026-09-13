using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Receives notifications about state events.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateObserver<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Observes a state-machine transition after the current state has been updated.</summary>
    /// <param name="context">The instance context of the state machine.</param>
    /// <param name="currentState">The current state (after the change).</param>
    /// <param name="previousState">The previous state (before the change).</param>
    /// <returns>A task that completes when the observer finishes processing the transition.</returns>
    Task StateChangedAsync(IBehaviorContext<TSaga> context, IState currentState, IState? previousState);
}
