using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class SagaConfigurationObservable :
    Connectable<ISagaConfigurationObserver>,
    ISagaConfigurationObserver
{
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.SagaConfigured(configurator));
    }

    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
        where TInstance : class, ISaga, SagaStateMachineInstance
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(stateMachine);

        ForEach(observer => observer.StateMachineSagaConfigured(configurator, stateMachine));
    }

    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class, ISaga
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.SagaMessageConfigured(configurator));
    }
}
