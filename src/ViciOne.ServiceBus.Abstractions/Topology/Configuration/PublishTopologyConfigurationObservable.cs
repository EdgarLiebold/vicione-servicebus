using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class PublishTopologyConfigurationObservable :
    Connectable<IPublishTopologyConfigurationObserver>,
    IPublishTopologyConfigurationObserver
{
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configurator));
    }
}
