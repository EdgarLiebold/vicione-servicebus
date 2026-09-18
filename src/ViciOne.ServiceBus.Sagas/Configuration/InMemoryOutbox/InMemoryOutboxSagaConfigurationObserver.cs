using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes in memory outbox saga configuration events.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class InMemoryOutboxSagaConfigurationObserver<TSaga> :
    ISagaConfigurationObserver
    where TSaga : class, ISaga
{
    readonly ISagaConfigurator<TSaga> _configurator;
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxSagaConfigurationObserver(IRegistrationContext context, ISagaConfigurator<TSaga> configurator,
        Action<IOutboxConfigurator>? configure)
        : this(
            context is null
                ? throw new ArgumentNullException(nameof(context))
                : context as ISetScopedConsumeContext
                    ?? throw new ArgumentException("The registration context must support scoped consume contexts.", nameof(context)),
            configurator,
            configure)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setter">The setter.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxSagaConfigurationObserver(ISetScopedConsumeContext? setter, ISagaConfigurator<TSaga> configurator,
        Action<IOutboxConfigurator>? configure)
    {
        _setter = setter;
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
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
        var specification = new InMemoryOutboxSpecification<TMessage>(_setter);

        _configure?.Invoke(specification);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }
}
