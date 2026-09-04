namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for receive transport.
/// </summary>
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
