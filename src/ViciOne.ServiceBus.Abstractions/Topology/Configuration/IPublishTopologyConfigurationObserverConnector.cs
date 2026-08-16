namespace ViciOne.ServiceBus.Configuration
{
    public interface IPublishTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectPublishTopologyConfigurationObserver(IPublishTopologyConfigurationObserver observer);
    }
}
