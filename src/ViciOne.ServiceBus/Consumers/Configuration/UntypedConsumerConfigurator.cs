using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures untyped consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class UntypedConsumerConfigurator<TConsumer> :
    IConsumerConfigurator,
    IReceiveEndpointSpecification
    where TConsumer : class
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IConsumerSpecification<TConsumer> _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="observer">The observer to connect.</param>
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
        return _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>Gets or sets the concurrency policy.</summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => _specification.ConcurrencyPolicy = value;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!typeof(TConsumer).ImplementsInterface<IConsumer>())
            yield return this.Warning($"The consumer class {TypeCache<TConsumer>.ShortName} does not implement any IMessageConsumer interfaces");

        foreach (var result in _specification.Validate())
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
