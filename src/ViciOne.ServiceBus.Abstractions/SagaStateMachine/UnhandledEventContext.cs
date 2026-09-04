using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// The context of an unhandled event in the state machine
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public interface UnhandledEventContext<TSaga> :
    BehaviorContext<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// The current state of the state machine
    /// </summary>
    State CurrentState { get; }

    /// <summary>
    /// Returns a Task that ignores the unhandled event
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task IgnoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a thrown exception task for the unhandled event
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ThrowAsync(CancellationToken cancellationToken = default);
}
