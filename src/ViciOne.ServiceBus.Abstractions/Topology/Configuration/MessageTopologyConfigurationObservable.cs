using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message topology configuration observable implementation.
/// </summary>
public class MessageTopologyConfigurationObservable :
    Connectable<IMessageTopologyConfigurationObserver>,
    IMessageTopologyConfigurationObserver
{
    /// <summary>
    /// Performs the message topology created operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configuration">The configuration callback.</param>
    public void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
        where T : class
    {
        ForEach(observer => observer.MessageTopologyCreated(configuration));
    }
}
