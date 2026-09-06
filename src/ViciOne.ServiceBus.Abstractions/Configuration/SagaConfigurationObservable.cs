using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for saga configuration.</summary>
public class SagaConfigurationObservable :
    Connectable<ISagaConfigurationObserver>,
    ISagaConfigurationObserver
{
    /// <summary>Reports that saga has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.SagaConfigured(configurator));
    }

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(stateMachine);

        ForEach(observer => observer.StateMachineSagaConfigured(configurator, stateMachine));
    }

    /// <summary>Reports that saga message has been configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.SagaMessageConfigured(configurator));
    }
}
