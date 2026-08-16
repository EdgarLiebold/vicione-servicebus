namespace ViciOne.ServiceBus.SagaStateMachine
{
    public interface IBehaviorBuilder<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        void Add(IStateMachineActivity<TInstance> activity);
    }
}
