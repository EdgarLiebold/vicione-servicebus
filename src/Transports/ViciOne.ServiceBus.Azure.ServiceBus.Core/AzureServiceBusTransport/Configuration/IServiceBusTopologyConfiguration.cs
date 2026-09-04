using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusTopologyConfiguration :
    ITopologyConfiguration
{
    new IServiceBusPublishTopologyConfigurator Publish { get; }

    new IServiceBusSendTopologyConfigurator Send { get; }

    new IServiceBusConsumeTopologyConfigurator Consume { get; }
}
