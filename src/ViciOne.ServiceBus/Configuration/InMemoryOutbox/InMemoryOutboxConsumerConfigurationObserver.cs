using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory outbox consumer configuration observer implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class InMemoryOutboxConsumerConfigurationObserver<TConsumer> :
    IConsumerConfigurationObserver
    where TConsumer : class
{
    readonly IConsumerConfigurator<TConsumer> _configurator;
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public InMemoryOutboxConsumerConfigurationObserver(IRegistrationContext context, IConsumerConfigurator<TConsumer> configurator,
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
