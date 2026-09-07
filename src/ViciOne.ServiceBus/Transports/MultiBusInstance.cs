using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts a named bus interface and its registration instance to the common bus-instance contract.</summary>
/// <typeparam name="TBus">The interface that uniquely identifies the bus registration.</typeparam>
public class MultiBusInstance<TBus> :
    IBusInstance<TBus>
    where TBus : IBus
{
    readonly TBus _bus;

    /// <summary>Creates an adapter for a typed bus and its underlying registration instance.</summary>
    /// <param name="bus">The typed bus exposed by this instance.</param>
    /// <param name="instance">The registration instance that owns lifecycle and endpoint operations.</param>
    public MultiBusInstance(TBus bus, IBusInstance instance)
    {
        BusInstance = instance;
        _bus = bus;
    }

    /// <summary>Gets the stable health and registration name derived from <typeparamref name="TBus" />.</summary>
    public string Name { get; } = FormatBusName();
    /// <summary>Gets the interface type that identifies this bus.</summary>
    public Type InstanceType => typeof(TBus);
    /// <summary>Gets the bus through the untyped contract.</summary>
    public IBus Bus => _bus;
    /// <summary>Gets the underlying registration instance.</summary>
    public IBusInstance BusInstance { get; }
    /// <summary>Gets the lifecycle control owned by the underlying instance.</summary>
    public IBusControl BusControl => BusInstance.BusControl;
    /// <summary>Gets the host configuration owned by the underlying instance.</summary>
    public IHostConfiguration HostConfiguration => BusInstance.HostConfiguration;

    TBus IBusInstance<TBus>.Bus => _bus;

    /// <summary>Connects a rider through the underlying bus instance.</summary>
    /// <typeparam name="TRider">The rider contract used to identify the registration.</typeparam>
    /// <param name="riderControl">The rider lifecycle controller.</param>
    public void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider
    {
        BusInstance.Connect<TRider>(riderControl);
    }

    /// <summary>Gets a rider from the underlying bus instance.</summary>
    /// <typeparam name="TRider">The rider contract used to identify the registration.</typeparam>
    /// <returns>The registered rider.</returns>
    public TRider GetRider<TRider>()
        where TRider : IRider
    {
        return BusInstance.GetRider<TRider>();
    }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name.</param>
    /// <param name="configure">An optional callback that configures the endpoint through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter endpointNameFormatter,
        Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
    {
        return BusInstance.ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects a receive endpoint for a named transport queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configure">An optional callback that configures the endpoint through registered services.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
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
