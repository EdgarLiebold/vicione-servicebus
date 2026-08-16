namespace ViciOne.ServiceBus.Configuration
{
    public interface ISendTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectSendTopologyConfigurationObserver(ISendTopologyConfigurationObserver observer);
    }
}
