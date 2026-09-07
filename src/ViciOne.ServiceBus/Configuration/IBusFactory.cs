using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Validates a transport configuration and creates the receive endpoint owned by its bus.</summary>
public interface IBusFactory :
    ISpecification
{
    /// <summary>Creates the temporary receive-endpoint configuration used by the bus runtime.</summary>
    /// <param name="configure">The callback that applies runtime-owned endpoint settings.</param>
    /// <returns>The configured bus receive endpoint.</returns>
    IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure);
}
