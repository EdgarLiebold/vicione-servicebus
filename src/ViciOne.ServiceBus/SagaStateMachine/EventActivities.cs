using System.Collections.Generic;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus;

public interface EventActivities<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders();
}
