
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Receives notifications about saga configuration events.</summary>
public interface ISagaConfigurationObserver
{
    /// <summary>Called immediately after the saga configuration is completed, but before the saga pipeline is built.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <param name="configurator">The completed saga configuration.</param>
    void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class;

    /// <summary>
    /// Called immediately after the state machine saga configuration is completed, but before the saga pipeline is built.
    /// <see cref="SagaConfigured{TInstance}" /> is also called so that policies applying to every saga are composed with
    /// state-machine-specific policies.
    /// </summary>
    /// <typeparam name="TInstance">The saga state managed by the state machine.</typeparam>
    /// <param name="configurator">The completed saga configuration.</param>
    /// <param name="stateMachine">The state machine that owns the saga configuration.</param>
    void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class;

    /// <summary>Called after the saga/message configuration is completed, but before the saga/message pipeline is built.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The completed message-specific saga configuration.</param>
    void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class
        where TMessage : class;
}
