using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for endpoint configuration.</summary>
public class EndpointConfigurationObservable :
    Connectable<IEndpointConfigurationObserver>,
    IEndpointConfigurationObserver
{
    /// <summary>Reports that endpoint has been configured.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        ForEach(observer => observer.EndpointConfigured(configurator));
    }
}
