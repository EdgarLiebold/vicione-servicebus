using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a multi bus instance implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class MultiBusInstance<TBus> :
    IBusInstance<TBus>
    where TBus : IBus
{
    readonly TBus _bus;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="instance">The instance value.</param>
    public MultiBusInstance(TBus bus, IBusInstance instance)
    {
        BusInstance = instance;
        _bus = bus;
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name { get; } = FormatBusName();
    /// <summary>
    /// Gets the instance type value.
    /// </summary>
    public Type InstanceType => typeof(TBus);
    /// <summary>
    /// Gets the bus value.
    /// </summary>
    public IBus Bus => _bus;
    /// <summary>
    /// Gets the bus instance value.
    /// </summary>
    public IBusInstance BusInstance { get; }
    /// <summary>
    /// Gets the bus control value.
    /// </summary>
    public IBusControl BusControl => BusInstance.BusControl;
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    public IHostConfiguration HostConfiguration => BusInstance.HostConfiguration;

    TBus IBusInstance<TBus>.Bus => _bus;

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <param name="riderControl">The rider control value.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        BusInstance.Connect<TRider>(riderControl);
    }

    /// <summary>
    /// Gets rider.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        return BusInstance.GetRider<TRider>();
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
        return BusInstance.ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
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
        return BusInstance.ConnectReceiveEndpoint(queueName, configure);
    }

    static string FormatBusName()
    {
        var name = typeof(TBus).Name;
        if (name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name.Substring(1);

        return $"vicione-servicebus-{KebabCaseEndpointNameFormatter.Instance.SanitizeName(name)}";
    }
}
