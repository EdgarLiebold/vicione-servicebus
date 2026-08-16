namespace ViciOne.ServiceBus
{
    public interface IActivityConfigurationObserverConnector
    {
        ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer);
    }
}
