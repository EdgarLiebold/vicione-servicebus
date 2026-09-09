using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes one configured bus, its host configuration, and its transport lifecycle.</summary>
public interface IBusInstance :
    IReceiveEndpointConnector
{
    /// <summary>Gets the registration name of this bus instance.</summary>
    string Name { get; }
    /// <summary>Gets the bus contract type represented by this instance.</summary>
    Type InstanceType { get; }

    /// <summary>Gets the application-facing bus endpoint.</summary>
    IBus Bus { get; }
    /// <summary>Gets the lifecycle and health control surface.</summary>
    IBusControl BusControl { get; }

    /// <summary>Gets the transport host configuration owned by the bus.</summary>
    IHostConfiguration HostConfiguration { get; }

    /// <summary>Attaches a configured rider to the bus lifecycle.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <param name="riderControl">The rider lifecycle controller to attach.</param>
    void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider;

    /// <summary>Gets the rider registered for the requested rider contract.</summary>
    /// <typeparam name="TRider">The rider type.</typeparam>
    /// <returns>The registered rider.</returns>
    TRider GetRider<TRider>()
        where TRider : IRider;
}


/// <summary>Exposes a configured bus through its strongly typed bus contract.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IBusInstance<out TBus> :
    IBusInstance
    where TBus : IBus
{
    /// <summary>Gets the strongly typed application-facing bus endpoint.</summary>
    new TBus Bus { get; }

    /// <summary>Gets the underlying untyped instance that owns the transport lifecycle.</summary>
    IBusInstance BusInstance { get; }
}
