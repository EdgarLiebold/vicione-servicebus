using System.ComponentModel;

namespace ViciOne.ServiceBus;

public interface IConsumerConfigurationObserverConnector
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer);
}
