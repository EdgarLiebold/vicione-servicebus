using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Coordinates a service instance's dedicated endpoint with its shared receive endpoints.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public sealed class ServiceInstanceConfigurator<TEndpointConfigurator> :
    IServiceInstanceConfigurator<TEndpointConfigurator>
    where TEndpointConfigurator : IReceiveEndpointConfigurator
{
    readonly ServiceInstanceOptions _options;

    /// <summary>Creates a service-instance configuration rooted at its dedicated receive endpoint.</summary>
    /// <param name="configurator">The bus-level configurator used to create shared receive endpoints.</param>
    /// <param name="options">The service-instance naming and endpoint options.</param>
    /// <param name="instanceEndpointConfigurator">The dedicated endpoint on which this instance is addressed.</param>
    public ServiceInstanceConfigurator(IReceiveConfigurator<TEndpointConfigurator> configurator, ServiceInstanceOptions options,
        TEndpointConfigurator instanceEndpointConfigurator)
    {
        BusConfigurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        InstanceEndpointConfigurator = instanceEndpointConfigurator
            ?? throw new ArgumentNullException(nameof(instanceEndpointConfigurator), "A service instance requires a dedicated receive endpoint.");
    }

    /// <summary>Gets the input address of the dedicated instance endpoint.</summary>
    public Uri InstanceAddress => InstanceEndpointConfigurator.InputAddress;

    IReceiveConfigurator IServiceInstanceConfigurator.BusConfigurator => BusConfigurator;
    IReceiveEndpointConfigurator IServiceInstanceConfigurator.InstanceEndpointConfigurator => InstanceEndpointConfigurator;

    /// <summary>Gets the bus-level configurator used to create shared receive endpoints.</summary>
    public IReceiveConfigurator<TEndpointConfigurator> BusConfigurator { get; }
    /// <summary>Gets the dedicated receive-endpoint configuration for this instance.</summary>
    public TEndpointConfigurator InstanceEndpointConfigurator { get; }

    /// <summary>Adds validation owned by the dedicated instance endpoint.</summary>
    /// <param name="specification">The specification whose validation results are projected onto the endpoint.</param>
    public void AddSpecification(ISpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        InstanceEndpointConfigurator.AddEndpointSpecification(new ValidateSpecification(specification));
    }

    /// <summary>Gets the formatter used when endpoint definitions derive queue names.</summary>
    public IEndpointNameFormatter EndpointNameFormatter => _options.EndpointNameFormatter;

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="configure">An optional callback applied to the newly created options.</param>
    /// <returns>The configured options instance.</returns>
    public TOptions Options<TOptions>(Action<TOptions>? configure = null)
        where TOptions : IOptions, new()
    {
        return _options.Options(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">An optional callback applied after the options are added.</param>
    /// <returns>The configured options instance.</returns>
    public TOptions Options<TOptions>(TOptions options, Action<TOptions>? configure = null)
        where TOptions : IOptions
    {
        return _options.Options(options, configure);
    }

    /// <summary>Attempts to get the configured options of the requested type.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="options">Receives the configured options when present.</param>
    /// <returns><see langword="true" /> when options of the requested type are configured.</returns>
    public bool TryGetOptions<TOptions>(out TOptions options)
        where TOptions : IOptions
    {
        return _options.TryGetOptions(out options);
    }

    /// <summary>Selects options.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <returns>The selected options.</returns>
    public IEnumerable<TOptions> SelectOptions<TOptions>()
        where TOptions : class
    {
        return _options.SelectOptions<TOptions>();
    }

    void IReceiveConfigurator.ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        ReceiveEndpoint(definition, endpointNameFormatter, x => configureEndpoint?.Invoke(x));
    }

    /// <summary>Adds a shared receive endpoint that depends on the dedicated instance endpoint.</summary>
    /// <param name="definition">The endpoint definition that supplies the shared queue settings.</param>
    /// <param name="endpointNameFormatter">An optional formatter that overrides the service-instance default.</param>
    /// <param name="configureEndpoint">An optional callback that configures the shared endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<TEndpointConfigurator>? configureEndpoint)
    {
        ArgumentNullException.ThrowIfNull(definition);

        endpointNameFormatter ??= EndpointNameFormatter;

        BusConfigurator.ReceiveEndpoint(definition, endpointNameFormatter, endpointConfigurator =>
        {
            endpointConfigurator.AddDependency(InstanceEndpointConfigurator);

            configureEndpoint?.Invoke(endpointConfigurator);
        });
    }

    void IReceiveConfigurator.ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        ArgumentNullException.ThrowIfNull(configureEndpoint);

        ReceiveEndpoint(queueName, x => configureEndpoint(x));
    }

    /// <summary>Adds a named shared receive endpoint that depends on the dedicated instance endpoint.</summary>
    /// <param name="queueName">The non-empty queue name of the shared endpoint.</param>
    /// <param name="configureEndpoint">An optional callback that configures the shared endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<TEndpointConfigurator>? configureEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

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
        ArgumentNullException.ThrowIfNull(observer);

        return BusConfigurator.ConnectEndpointConfigurationObserver(observer);
    }


    sealed class ValidateSpecification :
        IReceiveEndpointSpecification
    {
        readonly ISpecification _specification;

        public ValidateSpecification(ISpecification specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return _specification.Validate();
        }

        public void Configure(IReceiveEndpointBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
        }
    }
}
