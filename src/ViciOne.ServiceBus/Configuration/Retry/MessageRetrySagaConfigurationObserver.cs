using System;
using System.Threading;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a message retry for a saga, on the saga configurator, which is constrained to
/// the message types for that saga, and only applies to the saga prior to the saga repository.
/// </summary>
/// <typeparam name="TSaga">The saga type</typeparam>
public class MessageRetrySagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly CancellationToken _cancellationToken;
    readonly ISagaConfigurator<TSaga> _configurator;
    readonly Action<IRetryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="configure">The configuration callback.</param>
    public MessageRetrySagaConfigurationObserver(ISagaConfigurator<TSaga> configurator, CancellationToken cancellationToken,
        Action<IRetryConfigurator> configure)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _cancellationToken = cancellationToken;
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
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
        var specification = new ConsumeContextRetryPipeSpecification<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(Factory, _cancellationToken);

        _configure(specification);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }

    static RetryConsumeContext<TMessage> Factory<TMessage>(ConsumeContext<TMessage> context, IRetryPolicy retryPolicy, RetryContext? retryContext)
        where TMessage : class
    {
        return new RetryConsumeContext<TMessage>(context, retryPolicy, retryContext);
    }
}
