using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface SagaStateMachine<TSaga> :
    StateMachine<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Returns the event correlations for the state machine
    /// </summary>
    IEnumerable<EventCorrelation> Correlations { get; }

    /// <summary>
    /// Returns true if the saga state machine instance is complete and can be removed from the repository
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<bool> IsCompletedAsync(BehaviorContext<TSaga> context, CancellationToken cancellationToken = default);
}
