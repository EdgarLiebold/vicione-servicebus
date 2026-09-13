using System.Collections.Generic;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides the activity binders composed for one state-machine event.</summary>
/// <typeparam name="TInstance">The saga state type.</typeparam>
public interface IEventActivities<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Returns the activity binders in execution order.</summary>
    /// <returns>The configured activity binders.</returns>
    IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders();
}
