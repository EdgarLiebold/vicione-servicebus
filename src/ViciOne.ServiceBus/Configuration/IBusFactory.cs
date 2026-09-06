using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates bus instances.</summary>
public interface IBusFactory :
    ISpecification
{
    /// <summary>Create the bus endpoint configuration, which is used to create the bus.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created bus endpoint configuration.</returns>
    IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure);
}
