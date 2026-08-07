// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Collections.Generic;
    using Configuration;


    public partial class ViciOneServiceBusStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        class StateMachineSagaSpecification :
            SagaSpecification<TInstance>
        {
            readonly SagaStateMachine<TInstance> _stateMachine;

            public StateMachineSagaSpecification(SagaStateMachine<TInstance> stateMachine,
                IEnumerable<ISagaMessageSpecification<TInstance>> messageSpecifications)
                : base(messageSpecifications)
            {
                _stateMachine = stateMachine;
            }

            public override IEnumerable<ValidationResult> Validate()
            {
                Observers.ForEach(observer => observer.StateMachineSagaConfigured(this, _stateMachine));

                return base.Validate();
            }
        }
    }
}
