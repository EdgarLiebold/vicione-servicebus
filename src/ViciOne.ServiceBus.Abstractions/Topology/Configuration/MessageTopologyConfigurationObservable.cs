using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class MessageTopologyConfigurationObservable :
    Connectable<IMessageTopologyConfigurationObserver>,
    IMessageTopologyConfigurationObserver
{
    public void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configuration));
    }
}
