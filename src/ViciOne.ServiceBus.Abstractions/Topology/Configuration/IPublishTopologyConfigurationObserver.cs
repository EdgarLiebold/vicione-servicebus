namespace ViciOne.ServiceBus.Configuration;

public interface IPublishTopologyConfigurationObserver
{
    void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class;
}
