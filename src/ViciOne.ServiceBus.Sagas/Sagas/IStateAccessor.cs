using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Reads and writes a saga's current state and builds predicates for its stored representation.</summary>
/// <typeparam name="TSaga">The saga instance type.</typeparam>
public interface IStateAccessor<TSaga> :
    IProbeSite
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Reads the current state of the saga in the behavior context.</summary>
    /// <remarks>An accessor may initialize a missing state as part of the read.</remarks>
    /// <param name="context">The behavior context containing the saga instance.</param>
    /// <param name="cancellationToken">Cancellation requested for the accessor operation.</param>
    /// <returns>A task containing the current state, or null if the accessor reports no current state.</returns>
    Task<IState<TSaga>?> GetAsync(IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Stores the supplied state for the saga in the behavior context.</summary>
    /// <param name="context">The behavior context containing the saga instance.</param>
    /// <param name="state">The state to store as the saga's current state.</param>
    /// <param name="cancellationToken">Cancellation requested for the accessor operation.</param>
    /// <returns>A task representing the state update.</returns>
    Task SetAsync(IBehaviorContext<TSaga> context, IState<TSaga> state, CancellationToken cancellationToken = default);

    /// <summary>Builds a predicate that matches any supplied state using the saga's stored state representation.</summary>
    /// <param name="states">One or more states to match.</param>
    /// <returns>An expression that tests the saga's current state against the supplied states.</returns>
    Expression<Func<TSaga, bool>> GetStateExpression(params IState[] states);
}
