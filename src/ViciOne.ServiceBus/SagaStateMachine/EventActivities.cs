// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Collections.Generic;
    using SagaStateMachine;


    public interface EventActivities<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        IEnumerable<IActivityBinder<TInstance>> GetStateActivityBinders();
    }
}
