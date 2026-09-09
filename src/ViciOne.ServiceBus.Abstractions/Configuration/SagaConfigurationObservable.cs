using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for saga configuration.</summary>
public class SagaConfigurationObservable :
    Connectable<ISagaConfigurationObserver>,
    ISagaConfigurationObserver
{
    /// <summary>Notifies observers that a saga is configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The completed saga configuration.</param>
    public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.SagaConfigured(configurator));
    }

    /// <summary>Notifies observers that a state-machine saga is configured.</summary>
    /// <typeparam name="TInstance">The saga state managed by the state machine.</typeparam>
    /// <param name="configurator">The completed saga configuration.</param>
    /// <param name="stateMachine">The state machine that owns the saga configuration.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(stateMachine);

        ForEach(observer => observer.StateMachineSagaConfigured(configurator, stateMachine));
    }

    /// <summary>Notifies observers that a saga's message-specific pipeline is configured.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The completed message-specific saga configuration.</param>
    public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.SagaMessageConfigured(configurator));
    }
}
