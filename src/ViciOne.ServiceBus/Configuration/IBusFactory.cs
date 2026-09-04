using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for bus factory.
/// </summary>
public interface IBusFactory :
    ISpecification
{
    /// <summary>
    /// Create the bus endpoint configuration, which is used to create the bus
    /// </summary>
    /// <returns></returns>
    IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure);
}
