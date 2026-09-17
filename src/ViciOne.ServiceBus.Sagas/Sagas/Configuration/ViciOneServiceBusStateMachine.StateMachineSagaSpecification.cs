using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    class StateMachineSagaSpecification :
        SagaSpecification<TInstance>
    {
        readonly ISagaStateMachine<TInstance> _stateMachine;
        readonly ConfigurationObserverNotification _stateMachineConfigurationNotification = new ConfigurationObserverNotification();

        public StateMachineSagaSpecification(ISagaStateMachine<TInstance> stateMachine,
            IEnumerable<ISagaMessageSpecification<TInstance>> messageSpecifications)
            : base(EnsureMessageSpecifications(messageSpecifications))
        {
            ArgumentNullException.ThrowIfNull(stateMachine);

            _stateMachine = stateMachine;
        }

        public override IEnumerable<ValidationResult> Validate()
        {
            _stateMachineConfigurationNotification.EnsureNotified(() =>
                Observers.ForEach(observer => observer.StateMachineSagaConfigured(this, _stateMachine)));

            return base.Validate().ToArray();
        }

        static IEnumerable<ISagaMessageSpecification<TInstance>> EnsureMessageSpecifications(
            IEnumerable<ISagaMessageSpecification<TInstance>> messageSpecifications)
        {
            ArgumentNullException.ThrowIfNull(messageSpecifications);
            return messageSpecifications;
        }
    }
}
