// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SagaStateMachine
{
    public interface IBehaviorBuilder<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        void Add(IStateMachineActivity<TInstance> activity);
    }
}
