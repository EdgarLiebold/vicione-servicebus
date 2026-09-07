using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by saga state machine.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface SagaStateMachine<TSaga> :
    StateMachine<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Returns the event correlations for the state machine.</summary>
    IEnumerable<EventCorrelation> Correlations { get; }

    /// <summary>Returns true if the saga state machine instance is complete and can be removed from the repository.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the is completed outcome.</returns>
    Task<bool> IsCompletedAsync(BehaviorContext<TSaga> context, CancellationToken cancellationToken = default);
}
