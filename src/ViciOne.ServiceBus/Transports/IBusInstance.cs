using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for bus instance.
/// </summary>
public interface IBusInstance :
    IReceiveEndpointConnector
{
    /// <summary>
    /// Gets the name value.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Gets the instance type value.
    /// </summary>
    Type InstanceType { get; }

    /// <summary>
    /// Gets the bus value.
    /// </summary>
    IBus Bus { get; }
    /// <summary>
    /// Gets the bus control value.
    /// </summary>
    IBusControl BusControl { get; }

    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    IHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <param name="riderControl">The rider control value.</param>
    void Connect<TRider>(IRiderControl riderControl)
        where TRider : IRider;

    /// <summary>
    /// Gets rider.
    /// </summary>
    /// <typeparam name="TRider">The t rider type.</typeparam>
    /// <returns>The result of the operation.</returns>
    TRider GetRider<TRider>()
        where TRider : IRider;
}


/// <summary>
/// Defines the contract for bus instance.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public interface IBusInstance<out TBus> :
    IBusInstance
    where TBus : IBus
{
    /// <summary>
    /// Gets the bus value.
    /// </summary>
    new TBus Bus { get; }

    /// <summary>
    /// The original bus instance (since this is wrapped inside a multi-bus instance
    /// </summary>
    IBusInstance BusInstance { get; }
}
