using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an endpoint configuration observable implementation.
/// </summary>
public class EndpointConfigurationObservable :
    Connectable<IEndpointConfigurationObserver>,
    IEndpointConfigurationObserver
{
    /// <summary>
    /// Performs the endpoint configured operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void EndpointConfigured<T>(T configurator)
        where T : IReceiveEndpointConfigurator
    {
        ForEach(observer => observer.EndpointConfigured(configurator));
    }
}
