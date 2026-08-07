// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IReceiveObserverConnector
    {
        /// <summary>
        /// Connect an observer to the receiving endpoint
        /// </summary>
        /// <param name="observer"></param>
        /// <returns></returns>
        ConnectHandle ConnectReceiveObserver(IReceiveObserver observer);
    }
}
