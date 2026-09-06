using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Publishes observations for send topology configuration.</summary>
public class SendTopologyConfigurationObservable :
    Connectable<ISendTopologyConfigurationObserver>,
    ISendTopologyConfigurationObserver
{
    /// <summary>Reports that message topology has been created.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configuration">The callback used to configure the component.</param>
    public void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configuration));
    }
}
