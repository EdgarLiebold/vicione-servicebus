// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.ComponentModel;


    public interface IHandlerConfigurationObserverConnector
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer);
    }
}
