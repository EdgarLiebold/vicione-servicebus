using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures service instance.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public class ServiceInstanceConfigurator<TEndpointConfigurator> :
    IServiceInstanceConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    readonly ServiceInstanceOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="instanceEndpointConfigurator">The instance endpoint configurator.</param>
    public ServiceInstanceConfigurator(IReceiveConfigurator<TEndpointConfigurator> configurator, ServiceInstanceOptions options,
        TEndpointConfigurator instanceEndpointConfigurator)
    {
        if (instanceEndpointConfigurator == null)
            throw new ArgumentNullException(nameof(instanceEndpointConfigurator), "Service instance now requires an instance endpoint");

        BusConfigurator = configurator;
        InstanceEndpointConfigurator = instanceEndpointConfigurator;
        _options = options;
    }

    /// <summary>Gets the instance address.</summary>
    public Uri InstanceAddress => InstanceEndpointConfigurator.InputAddress;

    IReceiveConfigurator IServiceInstanceConfigurator.BusConfigurator => BusConfigurator;
    IReceiveEndpointConfigurator IServiceInstanceConfigurator.InstanceEndpointConfigurator => InstanceEndpointConfigurator;

    /// <summary>Gets the bus configurator.</summary>
    public IReceiveConfigurator<TEndpointConfigurator> BusConfigurator { get; }
    /// <summary>Gets the instance endpoint configurator.</summary>
    public TEndpointConfigurator InstanceEndpointConfigurator { get; }

    /// <summary>Adds specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddSpecification(ISpecification specification)
    {
        InstanceEndpointConfigurator.AddEndpointSpecification(new ValidateSpecification(specification));
    }

    /// <summary>Gets the endpoint name formatter.</summary>
    public IEndpointNameFormatter EndpointNameFormatter => _options.EndpointNameFormatter;

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Options<T>(Action<T>? configure = null)
        where T : IOptions, new()
    {
        return _options.Options(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions
    {
        return _options.Options(options, configure);
    }

    /// <summary>Attempts to get options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">Receives the options produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        return _options.TryGetOptions(out options);
    }

    /// <summary>Selects options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The selected options.</returns>
    public IEnumerable<T> SelectOptions<T>()
        where T : class
    {
        return _options.SelectOptions<T>();
    }

    void IReceiveConfigurator.ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        ReceiveEndpoint(definition, endpointNameFormatter, x => configureEndpoint?.Invoke(x));
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<TEndpointConfigurator>? configureEndpoint)
    {
        endpointNameFormatter ??= EndpointNameFormatter;

        BusConfigurator.ReceiveEndpoint(definition, endpointNameFormatter, endpointConfigurator =>
        {
            endpointConfigurator.AddDependency(InstanceEndpointConfigurator);

            configureEndpoint?.Invoke(endpointConfigurator);
        });
    }

    void IReceiveConfigurator.ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        ReceiveEndpoint(queueName, x => configureEndpoint(x));
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<TEndpointConfigurator>? configureEndpoint)
    {
        BusConfigurator.ReceiveEndpoint(queueName, endpointConfigurator =>
        {
            endpointConfigurator.AddDependency(InstanceEndpointConfigurator);

            configureEndpoint?.Invoke(endpointConfigurator);
        });
    }

    /// <summary>Connects endpoint configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return BusConfigurator.ConnectEndpointConfigurationObserver(observer);
    }


    class ValidateSpecification :
        IReceiveEndpointSpecification
    {
        readonly ISpecification _specification;

        public ValidateSpecification(ISpecification specification)
        {
            _specification = specification;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return _specification.Validate();
        }

        public void Configure(IReceiveEndpointBuilder builder)
        {
        }
    }
}
