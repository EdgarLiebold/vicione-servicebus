namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Defines the contract for behavior builder.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public interface IBehaviorBuilder<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    void Add(IStateMachineActivity<TInstance> activity);
}
