namespace ViciOne.ServiceBus
{
    public interface IReceiveTransportObserverConnector
    {
        ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer);
    }
}
