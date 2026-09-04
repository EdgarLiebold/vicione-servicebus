using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures scheduled message redelivery for a saga, on the saga configurator, which is constrained to
/// the message types for that saga, and only applies to the saga prior to the saga repository.
/// </summary>
/// <typeparam name="TSaga">The saga type</typeparam>
public class ScheduledRedeliverySagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly ISagaConfigurator<TSaga> _configurator;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public ScheduledRedeliverySagaConfigurationObserver(ISagaConfigurator<TSaga> configurator, Action<IRetryConfigurator> configure)
    {
        _configurator = configurator;
        _configure = configure;
    }

    void ISagaConfigurationObserver.SagaConfigured<T>(ISagaConfigurator<T> configurator)
    {
    }

    /// <summary>
    /// Performs the state machine saga configured operation.
    /// </summary>
    /// <typeparam name="TInstance">The t instance type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="stateMachine">The state machine value.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, SagaStateMachine<TInstance> stateMachine)
        where TInstance : class, ISaga, SagaStateMachineInstance
    {
    }

    void ISagaConfigurationObserver.SagaMessageConfigured<T, TMessage>(ISagaMessageConfigurator<T, TMessage> configurator)
    {
        var redeliverySpecification = new ScheduledRedeliveryPipeSpecification<TMessage>();
        var retrySpecification = new RedeliveryRetryPipeSpecification<TMessage>(redeliverySpecification);

        _configure?.Invoke(retrySpecification);

        _configurator.Message<TMessage>(x =>
        {
            x.AddPipeSpecification(redeliverySpecification);
            x.AddPipeSpecification(retrySpecification);
        });
    }
}
