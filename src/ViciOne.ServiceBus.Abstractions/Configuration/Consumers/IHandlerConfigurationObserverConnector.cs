using System.ComponentModel;

namespace ViciOne.ServiceBus;

public interface IHandlerConfigurationObserverConnector
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer);
}
