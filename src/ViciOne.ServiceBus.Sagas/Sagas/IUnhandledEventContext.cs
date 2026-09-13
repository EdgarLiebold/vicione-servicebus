using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides the state and behavior context for an event without a configured activity.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface IUnhandledEventContext<TSaga> :
    IBehaviorContext<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the state in which the event was unhandled.</summary>
    IState CurrentState { get; }

    /// <summary>Accepts the unhandled event without executing an activity.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IgnoreAsync(CancellationToken cancellationToken = default);

    /// <summary>Rejects the unhandled event with the configured state-machine exception.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ThrowAsync(CancellationToken cancellationToken = default);
}
