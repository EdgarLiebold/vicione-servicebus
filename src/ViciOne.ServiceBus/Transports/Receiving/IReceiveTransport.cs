namespace ViciOne.ServiceBus.Transports;

/// <summary>Starts and observes a transport-specific receive pipeline.</summary>
public interface IReceiveTransport :
    IReceiveObserverConnector,
    IPublishObserverConnector,
    ISendObserverConnector,
    IReceiveTransportObserverConnector,
    IProbeSite
{
    /// <summary>Starts receiving messages through the configured transport pipeline.</summary>
    /// <returns>A handle that controls the active transport generation.</returns>
    ReceiveTransportHandle Start();
}
