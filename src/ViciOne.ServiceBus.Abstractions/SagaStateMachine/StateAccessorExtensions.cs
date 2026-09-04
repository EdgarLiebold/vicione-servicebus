using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public static class StateAccessorExtensions
{
    public static Task<State<TSaga>?> GetStateAsync<TSaga>(this IStateAccessor<TSaga> accessor, BehaviorContext<TSaga> context, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        return accessor.GetAsync(context, cancellationToken: cancellationToken);
    }

    public static Task<State<TSaga>?> GetStateAsync<TSaga>(this StateMachine<TSaga> accessor, BehaviorContext<TSaga> context, CancellationToken cancellationToken = default)
        where TSaga : class, SagaStateMachineInstance
    {
        return accessor.Accessor.GetAsync(context, cancellationToken: cancellationToken);
    }
}
