using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service instance configurator implementation.
/// </summary>
/// <typeparam name="TEndpointConfigurator">The t endpoint configurator type.</typeparam>
public class ServiceInstanceConfigurator<TEndpointConfigurator> :
    IServiceInstanceConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    readonly ServiceInstanceOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="instanceEndpointConfigurator">The instance endpoint configurator value.</param>
    public ServiceInstanceConfigurator(IReceiveConfigurator<TEndpointConfigurator> configurator, ServiceInstanceOptions options,
        TEndpointConfigurator instanceEndpointConfigurator)
    {
        if (instanceEndpointConfigurator == null)
            throw new ArgumentNullException(nameof(instanceEndpointConfigurator), "Service instance now requires an instance endpoint");

        BusConfigurator = configurator;
        InstanceEndpointConfigurator = instanceEndpointConfigurator;
        _options = options;
    }

    /// <summary>
    /// Gets the instance address value.
    /// </summary>
    public Uri InstanceAddress => InstanceEndpointConfigurator.InputAddress;

    IReceiveConfigurator IServiceInstanceConfigurator.BusConfigurator => BusConfigurator;
    IReceiveEndpointConfigurator IServiceInstanceConfigurator.InstanceEndpointConfigurator => InstanceEndpointConfigurator;

    /// <summary>
    /// Gets the bus configurator value.
    /// </summary>
    public IReceiveConfigurator<TEndpointConfigurator> BusConfigurator { get; }
    /// <summary>
    /// Gets the instance endpoint configurator value.
    /// </summary>
    public TEndpointConfigurator InstanceEndpointConfigurator { get; }

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddSpecification(ISpecification specification)
    {
        InstanceEndpointConfigurator.AddEndpointSpecification(new ValidateSpecification(specification));
    }

    /// <summary>
    /// Gets the endpoint name formatter value.
    /// </summary>
    public IEndpointNameFormatter EndpointNameFormatter => _options.EndpointNameFormatter;

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public T Options<T>(Action<T>? configure = null)
        where T : IOptions, new()
    {
        return _options.Options(configure);
    }

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions
    {
        return _options.Options(options, configure);
    }

    /// <summary>
    /// Attempts to get options.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        return _options.TryGetOptions(out options);
    }

    /// <summary>
    /// Performs the select options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(string queueName, Action<TEndpointConfigurator>? configureEndpoint)
    {
        BusConfigurator.ReceiveEndpoint(queueName, endpointConfigurator =>
        {
            endpointConfigurator.AddDependency(InstanceEndpointConfigurator);

            configureEndpoint?.Invoke(endpointConfigurator);
        });
    }

    /// <summary>
    /// Connects endpoint configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
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
