namespace ViciOne.ServiceBus;

public interface IReceiveEndpointObserverConnector
{
    ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer);
}
