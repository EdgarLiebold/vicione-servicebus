using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public static class StateAccessorExtensions
{
    public static Task<State<TSaga>> GetState<TSaga>(this IStateAccessor<TSaga> accessor, BehaviorContext<TSaga> context)
        where TSaga : class, SagaStateMachineInstance
    {
        return accessor.Get(context);
    }

    public static Task<State<TSaga>> GetState<TSaga>(this StateMachine<TSaga> accessor, BehaviorContext<TSaga> context)
        where TSaga : class, SagaStateMachineInstance
    {
        return accessor.Accessor.Get(context);
    }
}
