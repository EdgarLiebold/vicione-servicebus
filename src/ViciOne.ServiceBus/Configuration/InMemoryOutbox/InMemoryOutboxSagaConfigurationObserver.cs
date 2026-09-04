using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory outbox saga configuration observer implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class InMemoryOutboxSagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly ISagaConfigurator<TSaga> _configurator;
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public InMemoryOutboxSagaConfigurationObserver(IRegistrationContext context, ISagaConfigurator<TSaga> configurator,
        Action<IOutboxConfigurator>? configure)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)), configurator, configure)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="setter">The setter value.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public InMemoryOutboxSagaConfigurationObserver(ISetScopedConsumeContext? setter, ISagaConfigurator<TSaga> configurator,
        Action<IOutboxConfigurator>? configure)
    {
        _setter = setter;
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
        var specification = new InMemoryOutboxSpecification<TMessage>(_setter);

        _configure?.Invoke(specification);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }
}
