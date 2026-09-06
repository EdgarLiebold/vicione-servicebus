using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Represents an instance of multi bus.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class MultiBusInstance<TBus> :
    IBusInstance<TBus>
    where TBus : IBus
{
    readonly TBus _bus;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="instance">The instance.</param>
    public MultiBusInstance(TBus bus, IBusInstance instance)
    {
        BusInstance = instance;
        _bus = bus;
    }

    /// <summary>Gets the name.</summary>
    public string Name { get; } = FormatBusName();
    /// <summary>Gets the instance type.</summary>
    public Type InstanceType => typeof(TBus);
    /// <summary>Gets the bus.</summary>
    public IBus Bus => _bus;
    /// <summary>Gets the bus instance.</summary>
    public IBusInstance BusInstance { get; }
    /// <summary>Gets the bus control.</summary>
    public IBusControl BusControl => BusInstance.BusControl;
    /// <summary>Gets the host configuration.</summary>
    public IHostConfiguration HostConfiguration => BusInstance.HostConfiguration;

    TBus IBusInstance<TBus>.Bus => _bus;

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <param name="riderControl">The rider control.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        BusInstance.Connect<TRider>(riderControl);
    }

    /// <summary>Gets rider.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <returns>The rider.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        return BusInstance.GetRider<TRider>();
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        return BusInstance.ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
