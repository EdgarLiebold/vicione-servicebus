using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IStateAccessor<TSaga> :
    IProbeSite
    where TSaga : class, SagaStateMachineInstance
{
    Task<State<TSaga>?> GetAsync(BehaviorContext<TSaga> context, CancellationToken cancellationToken = default);

    Task SetAsync(BehaviorContext<TSaga> context, State<TSaga> state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a state expression to the instance current state property type.
    /// </summary>
    /// <param name="states"></param>
    /// <returns></returns>
    Expression<Func<TSaga, bool>> GetStateExpression(params State[] states);
}
