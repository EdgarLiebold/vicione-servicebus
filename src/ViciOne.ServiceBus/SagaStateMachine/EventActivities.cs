using System.Collections.Generic;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by event activities.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public interface EventActivities<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Gets state activity binders.</summary>
    /// <returns>The state activity binders.</returns>
    IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders();
}
