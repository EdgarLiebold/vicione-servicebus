namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Builds behavior components.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public interface IBehaviorBuilder<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="activity">The activity.</param>
    void Add(IStateMachineActivity<TInstance> activity);
}
