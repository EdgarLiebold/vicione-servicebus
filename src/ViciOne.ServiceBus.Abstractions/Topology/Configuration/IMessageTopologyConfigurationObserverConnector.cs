namespace ViciOne.ServiceBus.Configuration;

public interface IMessageTopologyConfigurationObserverConnector
{
    ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer);
}
