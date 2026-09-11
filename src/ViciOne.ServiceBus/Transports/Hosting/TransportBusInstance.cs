using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Combines a bus control, transport host, host configuration, and registration context into one bus instance.</summary>
/// <typeparam name="TEndpointConfigurator">The transport-specific receive-endpoint configurator.</typeparam>
public class TransportBusInstance<TEndpointConfigurator> :
    IBusInstance,
    IReceiveEndpointConnector<TEndpointConfigurator>
    where TEndpointConfigurator : class, IReceiveEndpointConfigurator
{
    readonly IHost<TEndpointConfigurator> _host;

    /// <summary>Creates a bus instance from its lifecycle control, transport host, and registration state.</summary>
    /// <param name="busControl">The lifecycle control exposed as the bus.</param>
    /// <param name="host">The transport host that owns endpoints and riders.</param>
    /// <param name="hostConfiguration">The active host configuration.</param>
    /// <param name="busRegistrationContext">The context that supplies registered endpoint configuration.</param>
    public TransportBusInstance(IBusControl busControl, IHost<TEndpointConfigurator> host, IHostConfiguration hostConfiguration, IBusRegistrationContext
        busRegistrationContext)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        RegistrationContext = busRegistrationContext ?? throw new ArgumentNullException(nameof(busRegistrationContext));

        BusControl = busControl ?? throw new ArgumentNullException(nameof(busControl));
        HostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
    }

    /// <summary>Gets the context used to configure dynamically connected endpoints.</summary>
    protected IBusRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the stable default bus registration name.</summary>
    public string Name => "vicione-servicebus-bus";
    /// <summary>Gets the bus contract type represented by this instance.</summary>
    public Type InstanceType => typeof(IBus);
    /// <summary>Gets the bus lifecycle control through the bus contract.</summary>
    public IBus Bus => BusControl;
    /// <summary>Gets the bus lifecycle control.</summary>
    public IBusControl BusControl { get; }

    /// <summary>Gets the active transport-host configuration.</summary>
    public IHostConfiguration HostConfiguration { get; }

    /// <summary>Adds a rider to the transport host.</summary>
    /// <typeparam name="TRider">The rider contract used to derive its registration name.</typeparam>
    /// <param name="riderControl">The rider lifecycle controller.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        ArgumentNullException.ThrowIfNull(riderControl);
        var name = GetRiderName<TRider>();
        _host.AddRider(name, riderControl);
    }

    /// <summary>Gets a rider registered with the transport host.</summary>
    /// <typeparam name="TRider">The rider contract used to derive its registration name.</typeparam>
    /// <returns>The registered rider.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        var name = GetRiderName<TRider>();
        return (TRider)_host.GetRider(name);
    }

    /// <summary>Connects a transport-neutral endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name.</param>
    /// <param name="configure">An optional callback that configures the endpoint through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(endpointNameFormatter);
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints()
                .Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects a transport-neutral endpoint for a named queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configure">An optional callback that configures the endpoint through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        return _host.ConnectReceiveEndpoint(queueName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(queueName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects a transport-specific endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name.</param>
    /// <param name="configure">An optional callback that applies transport-specific settings through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(endpointNameFormatter);
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects a transport-specific endpoint for a named queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configure">An optional callback that applies transport-specific settings through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        return _host.ConnectReceiveEndpoint(queueName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(queueName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    static string GetRiderName<TRider>()
        where TRider : IRider
    {
        return TypeCache<TRider>.ShortName;
    }
}
