using System.Collections.Generic;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for event activities.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public interface EventActivities<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Gets state activity binders.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders();
}
