namespace ViciOne.ServiceBus.Configuration;

public interface IConsumeTopologyConfigurationObserver
{
    void MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> configuration)
        where T : class;
}
