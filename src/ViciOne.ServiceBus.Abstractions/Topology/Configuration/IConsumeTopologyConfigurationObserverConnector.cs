namespace ViciOne.ServiceBus.Configuration
{
    public interface IConsumeTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer);
    }
}
