using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines event correlation and completion semantics for a saga state machine.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaStateMachine<TSaga> :
    IStateMachine<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets the message-to-saga correlations configured for the state machine.</summary>
    IEnumerable<IEventCorrelation> Correlations { get; }

    /// <summary>Determines whether the current saga instance has reached its terminal state.</summary>
    /// <param name="context">The behavior context containing the saga instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns <see langword="true" /> when the saga can be removed from its repository.</returns>
    Task<bool> IsCompletedAsync(IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default);
}
