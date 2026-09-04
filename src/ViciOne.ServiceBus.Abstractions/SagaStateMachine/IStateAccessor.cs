using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state accessor.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IStateAccessor<TSaga> :
    IProbeSite
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<State<TSaga>?> GetAsync(BehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="state">The state value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SetAsync(BehaviorContext<TSaga> context, State<TSaga> state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a state expression to the instance current state property type.
    /// </summary>
    /// <param name="states"></param>
    /// <returns></returns>
    Expression<Func<TSaga, bool>> GetStateExpression(params State[] states);
}
