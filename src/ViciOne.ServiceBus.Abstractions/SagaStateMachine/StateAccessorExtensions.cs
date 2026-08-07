// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;


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
}
