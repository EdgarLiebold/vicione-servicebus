// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SagaStateMachine
{
    public interface IStateEventFilter<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        bool Filter(BehaviorContext<TSaga> context);

        bool Filter<T>(BehaviorContext<TSaga, T> context)
            where T : class;
    }
}
