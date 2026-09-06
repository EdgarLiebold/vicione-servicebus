using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for publish topology configuration.</summary>
public class PublishTopologyConfigurationObservable :
    Connectable<IPublishTopologyConfigurationObserver>,
    IPublishTopologyConfigurationObserver
{
    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configurator));
    }
}
