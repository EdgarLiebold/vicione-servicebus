using System.ComponentModel;

namespace ViciOne.ServiceBus;

public interface ISagaConfigurationObserverConnector
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    ConnectHandle ConnectSagaConfigurationObserver(ISagaConfigurationObserver observer);
}
