using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Represents an instance of transport bus.</summary>
/// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
public class TransportBusInstance<TEndpointConfigurator> :
    IBusInstance,
    IReceiveEndpointConnector<TEndpointConfigurator>
    where TEndpointConfigurator : class, IReceiveEndpointConfigurator
{
    readonly IHost<TEndpointConfigurator> _host;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="busControl">The bus control.</param>
    /// <param name="host">The host.</param>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="busRegistrationContext">The bus registration context.</param>
    public TransportBusInstance(IBusControl busControl, IHost<TEndpointConfigurator> host, IHostConfiguration hostConfiguration, IBusRegistrationContext
        busRegistrationContext)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        RegistrationContext = busRegistrationContext;

        BusControl = busControl;
        HostConfiguration = hostConfiguration;
    }

    /// <summary>Gets the registration context.</summary>
    protected IBusRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the name.</summary>
    public string Name => "vicione-servicebus-bus";
    /// <summary>Gets the instance type.</summary>
    public Type InstanceType => typeof(IBus);
    /// <summary>Gets the bus.</summary>
    public IBus Bus => BusControl;
    /// <summary>Gets the bus control.</summary>
    public IBusControl BusControl { get; }

    /// <summary>Gets the host configuration.</summary>
    public IHostConfiguration HostConfiguration { get; }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <param name="riderControl">The rider control.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        var name = GetRiderName<TRider>();
        _host.AddRider(name, riderControl);
    }

    /// <summary>Gets rider.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <returns>The rider.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        var name = GetRiderName<TRider>();
        return (TRider)_host.GetRider(name);
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints()
                .Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        return _host.ConnectReceiveEndpoint(queueName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(queueName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null)
    {
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null)
    {
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
