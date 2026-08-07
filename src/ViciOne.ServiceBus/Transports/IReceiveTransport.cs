// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public interface IReceiveTransport :
        IReceiveObserverConnector,
        IPublishObserverConnector,
        ISendObserverConnector,
        IReceiveTransportObserverConnector,
        IProbeSite
    {
        /// <summary>
        /// Start receiving on a transport, sending messages to the specified pipe.
        /// </summary>
        /// <returns></returns>
        ReceiveTransportHandle Start();
    }
}
