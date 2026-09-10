using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a runtime-selected consumer type backed by an object factory.</summary>
/// <typeparam name="TConsumer">The runtime-selected consumer implementation to configure.</typeparam>
public sealed class UntypedConsumerConfigurator<TConsumer> :
    IConsumerConfigurator,
    IReceiveEndpointSpecification
    where TConsumer : class
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IConsumerSpecification<TConsumer> _specification;

    /// <summary>Creates endpoint configuration for a runtime-selected consumer implementation.</summary>
    /// <param name="consumerFactory">The delegate that creates the requested runtime consumer type.</param>
    /// <param name="observer">The endpoint observer notified as consumer configuration is completed.</param>
    public UntypedConsumerConfigurator(Func<Type, object> consumerFactory, IConsumerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(consumerFactory);
        ArgumentNullException.ThrowIfNull(observer);

        _consumerFactory = new DelegateConsumerFactory<TConsumer>(() => (TConsumer)consumerFactory(typeof(TConsumer)));

        _specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();

        _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Sets the maximum number of messages admitted concurrently for this consumer.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>Sets the consumer-wide concurrency policy.</summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => _specification.ConcurrencyPolicy = value;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (ValidationResult result in _consumerFactory.Validate())
            yield return result;

        foreach (ValidationResult result in _specification.Validate())
            yield return result;
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConsumerConnectorCache<TConsumer>.Connector.ConnectConsumer(builder, _consumerFactory, _specification);
    }
}
