using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an untyped consumer configurator implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class UntypedConsumerConfigurator<TConsumer> :
    IConsumerConfigurator,
    IReceiveEndpointSpecification
    where TConsumer : class
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IConsumerSpecification<TConsumer> _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="observer">The observer value.</param>
    public UntypedConsumerConfigurator(Func<Type, object> consumerFactory, IConsumerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(observer);

        _consumerFactory = new DelegateConsumerFactory<TConsumer>(() => (TConsumer)consumerFactory(typeof(TConsumer)));

        _specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();

        _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Gets or sets the concurrency policy value.
    /// </summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => _specification.ConcurrencyPolicy = value;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!typeof(TConsumer).ImplementsInterface<IConsumer>())
            yield return this.Warning($"The consumer class {TypeCache<TConsumer>.ShortName} does not implement any IMessageConsumer interfaces");

        foreach (var result in _specification.Validate())
            yield return result;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConsumerConnectorCache<TConsumer>.Connector.ConnectConsumer(builder, _consumerFactory, _specification);
    }
}
