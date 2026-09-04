namespace ViciOne.ServiceBus.Configuration;

public interface ISendTopologyConfigurationObserver
{
    void MessageTopologyCreated<T>(IMessageSendTopologyConfigurator<T> configuration)
        where T : class;
}
