using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes in memory outbox consumer configuration events.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class InMemoryOutboxConsumerConfigurationObserver<TConsumer> :
    IConsumerConfigurationObserver
    where TConsumer : class
{
    readonly IConsumerConfigurator<TConsumer> _configurator;
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxConsumerConfigurationObserver(IRegistrationContext context, IConsumerConfigurator<TConsumer> configurator,
        Action<IOutboxConfigurator>? configure)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)), configurator, configure)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setter">The setter.</param>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxConsumerConfigurationObserver(ISetScopedConsumeContext? setter, IConsumerConfigurator<TConsumer> configurator,
        Action<IOutboxConfigurator>? configure)
    {
        _setter = setter;
        _configurator = configurator;
        _configure = configure;
    }

    void IConsumerConfigurationObserver.ConsumerConfigured<T>(IConsumerConfigurator<T> configurator)
    {
    }

    void IConsumerConfigurationObserver.ConsumerMessageConfigured<T, TMessage>(IConsumerMessageConfigurator<T, TMessage> configurator)
    {
        var specification = new InMemoryOutboxSpecification<TMessage>(_setter);

        _configure?.Invoke(specification);

        _configurator.Message<TMessage>(x => x.AddPipeSpecification(specification));
    }
}
