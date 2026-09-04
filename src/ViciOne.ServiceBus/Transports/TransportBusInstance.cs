using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a transport bus instance implementation.
/// </summary>
/// <typeparam name="TEndpointConfigurator">The t endpoint configurator type.</typeparam>
public class TransportBusInstance<TEndpointConfigurator> :
    IBusInstance,
    IReceiveEndpointConnector<TEndpointConfigurator>
    where TEndpointConfigurator : class, IReceiveEndpointConfigurator
{
    readonly IHost<TEndpointConfigurator> _host;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busControl">The bus control value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busRegistrationContext">The bus registration context value.</param>
    public TransportBusInstance(IBusControl busControl, IHost<TEndpointConfigurator> host, IHostConfiguration hostConfiguration, IBusRegistrationContext
        busRegistrationContext)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        RegistrationContext = busRegistrationContext;

        BusControl = busControl;
        HostConfiguration = hostConfiguration;
    }

    /// <summary>
    /// Gets the registration context value.
    /// </summary>
    protected IBusRegistrationContext RegistrationContext { get; }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name => "vicione-servicebus-bus";
    /// <summary>
    /// Gets the instance type value.
    /// </summary>
    public Type InstanceType => typeof(IBus);
    /// <summary>
    /// Gets the bus value.
    /// </summary>
    public IBus Bus => BusControl;
    /// <summary>
    /// Gets the bus control value.
    /// </summary>
    public IBusControl BusControl { get; }

    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public IHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <param name="riderControl">The rider control value.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        var name = GetRiderName<TRider>();
        _host.AddRider(name, riderControl);
    }

    /// <summary>
    /// Gets rider.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        var name = GetRiderName<TRider>();
        return (TRider)_host.GetRider(name);
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        return _host.ConnectReceiveEndpoint(queueName, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(queueName, configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, TEndpointConfigurator>? configure = null)
    {
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configurator =>
        {
            RegistrationContext.GetConfigureReceiveEndpoints().Configure(definition.GetEndpointName(endpointNameFormatter), configurator);

            configure?.Invoke(RegistrationContext, configurator);
        });
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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
