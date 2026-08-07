// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Connect an observer that is notified when a message is sent to an endpoint
    /// </summary>
    public interface ISendObserverConnector
    {
        ConnectHandle ConnectSendObserver(ISendObserver observer);
    }
}
