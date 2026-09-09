using System;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for endpoint configuration.</summary>
public class EndpointConfigurationObservable :
    Connectable<IEndpointConfigurationObserver>,
    IEndpointConfigurationObserver
{
    /// <summary>Reports that a receive endpoint has been configured.</summary>
    /// <typeparam name="T">The transport-specific endpoint configurator type.</typeparam>
    /// <param name="configurator">The completed endpoint configuration.</param>
    public void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ForEach(observer => observer.EndpointConfigured(configurator));
    }
}
