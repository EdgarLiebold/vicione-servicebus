using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a publish topology configuration observable implementation.
/// </summary>
public class PublishTopologyConfigurationObservable :
    Connectable<IPublishTopologyConfigurationObserver>,
    IPublishTopologyConfigurationObserver
{
    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configurator));
    }
}
