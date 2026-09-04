namespace ViciOne.ServiceBus.Configuration;

public interface ISendPipeSpecificationObserverConnector
{
    ConnectHandle ConnectSendPipeSpecificationObserver(ISendPipeSpecificationObserver observer);
}
