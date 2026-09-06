using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by bus instance.</summary>
public interface IBusInstance :
    IReceiveEndpointConnector
{
    /// <summary>Gets the name.</summary>
    string Name { get; }
    /// <summary>Gets the instance type.</summary>
    Type InstanceType { get; }

    /// <summary>Gets the bus.</summary>
    IBus Bus { get; }
    /// <summary>Gets the bus control.</summary>
    IBusControl BusControl { get; }

    /// <summary>Gets the host configuration.</summary>
    IHostConfiguration HostConfiguration { get; }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <param name="riderControl">The rider control.</param>
    void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider;

    /// <summary>Gets rider.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <returns>The rider.</returns>
    TRider GetRider<TRider>()
        where TRider : IRider;
}


/// <summary>Defines the operations required by bus instance.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IBusInstance<out TBus> :
    IBusInstance
    where TBus : IBus
{
    /// <summary>Gets the bus.</summary>
    new TBus Bus { get; }

    /// <summary>The original bus instance (since this is wrapped inside a multi-bus instance.</summary>
    IBusInstance BusInstance { get; }
}
