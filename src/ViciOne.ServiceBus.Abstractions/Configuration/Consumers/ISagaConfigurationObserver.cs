
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga configuration observer.
/// </summary>
public interface ISagaConfigurationObserver
{
    /// <summary>
    /// Called immediately after the saga configuration is completed, but before the saga pipeline is built.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <param name="configurator"></param>
    void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
        where TSaga : class, ISaga;

    /// <summary>
    /// Called immediately after the state machine saga configuration is completed, but before the saga pipeline is built.
    /// <see cref="SagaConfigured{TInstance}" /> is also called so that policies applying to every saga are composed with
    /// state-machine-specific policies.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="stateMachine"></param>
    /// <typeparam name="TInstance"></typeparam>
    void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
        where TInstance : class, ISaga, SagaStateMachineInstance;

    /// <summary>
    /// Called after the saga/message configuration is completed, but before the saga/message pipeline is built.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    /// <param name="configurator"></param>
    void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
        where TSaga : class, ISaga
        where TMessage : class;
}
