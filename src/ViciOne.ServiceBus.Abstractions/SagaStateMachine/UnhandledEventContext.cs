using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>The context of an unhandled event in the state machine.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface UnhandledEventContext<TSaga> :
    BehaviorContext<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>The current state of the state machine.</summary>
    State CurrentState { get; }

    /// <summary>Returns a Task that ignores the unhandled event.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IgnoreAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a thrown exception task for the unhandled event.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ThrowAsync(CancellationToken cancellationToken = default);
}
