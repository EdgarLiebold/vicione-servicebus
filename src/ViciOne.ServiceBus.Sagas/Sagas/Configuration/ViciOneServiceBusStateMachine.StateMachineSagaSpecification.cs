using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    class StateMachineSagaSpecification :
        SagaSpecification<TInstance>
    {
        readonly SagaStateMachine<TInstance> _stateMachine;
        readonly ConfigurationObserverNotification _stateMachineConfigurationNotification = new ConfigurationObserverNotification();

        public StateMachineSagaSpecification(SagaStateMachine<TInstance> stateMachine,
            IEnumerable<ISagaMessageSpecification<TInstance>> messageSpecifications)
            : base(messageSpecifications)
        {
            _stateMachine = stateMachine;
        }

        public override IEnumerable<ValidationResult> Validate()
        {
            _stateMachineConfigurationNotification.EnsureNotified(() =>
                Observers.ForEach(observer => observer.StateMachineSagaConfigured(this, _stateMachine)));

            return base.Validate().ToArray();
        }
    }
}
