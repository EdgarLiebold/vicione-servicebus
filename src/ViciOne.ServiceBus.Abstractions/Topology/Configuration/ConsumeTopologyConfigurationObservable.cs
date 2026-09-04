using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

public class ConsumeTopologyConfigurationObservable :
    Connectable<IConsumeTopologyConfigurationObserver>,
    IConsumeTopologyConfigurationObserver
{
    public void MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> configuration)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configuration));
    }
}
