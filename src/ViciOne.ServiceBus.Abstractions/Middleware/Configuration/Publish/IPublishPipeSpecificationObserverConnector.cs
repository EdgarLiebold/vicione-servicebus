namespace ViciOne.ServiceBus.Configuration
{
    public interface IPublishPipeSpecificationObserverConnector
    {
        ConnectHandle ConnectPublishPipeSpecificationObserver(IPublishPipeSpecificationObserver observer);
    }
}
