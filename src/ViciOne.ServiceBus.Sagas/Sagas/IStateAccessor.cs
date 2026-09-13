using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state accessor.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateAccessor<TSaga> :
    IProbeSite
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Retrieves the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<IState<TSaga>?> GetAsync(IBehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="state">The state.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync(IBehaviorContext<TSaga> context, IState<TSaga> state, CancellationToken cancellationToken = default);

    /// <summary>Converts a state expression to the instance current state property type.</summary>
    /// <param name="states">The states.</param>
    /// <returns>The state expression.</returns>
    Expression<Func<TSaga, bool>> GetStateExpression(params IState[] states);
}
