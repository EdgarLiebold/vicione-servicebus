using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures scheduled message redelivery for a saga, on the saga configurator, which is constrained to
/// the message types for that saga, and only applies to the saga prior to the saga repository.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
public class ScheduledRedeliverySagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly ISagaConfigurator<TSaga> _configurator;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public ScheduledRedeliverySagaConfigurationObserver(ISagaConfigurator<TSaga> configurator, Action<IRetryConfigurator> configure)
    {
        _configurator = configurator;
        _configure = configure;
    }

    void ISagaConfigurationObserver.SagaConfigured<T>(ISagaConfigurator<T> configurator)
    {
    }

    /// <summary>Reports that state machine saga has been configured.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
        where TInstance : class
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
