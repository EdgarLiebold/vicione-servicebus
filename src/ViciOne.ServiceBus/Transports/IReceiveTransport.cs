namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by receive transport.</summary>
public interface IReceiveTransport :
    IReceiveObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    IReceiveTransportObserverConnector,
    IProbeSite
{
    /// <summary>Start receiving on a transport, sending messages to the specified pipe.</summary>
    /// <returns>The receive transport handle produced by the operation.</returns>
    ReceiveTransportHandle Start();
}
