using ViciOne.ServiceBus.AzureServiceBusTransport;

namespace ViciOne.ServiceBus;

public interface IServiceBusSendTopology :
    ISendTopology
{
    new IServiceBusMessageSendTopology<T> GetMessageTopology<T>()
        where T : class;

    SendSettings GetSendSettings(ServiceBusEndpointAddress address);

    SendSettings GetErrorSettings(IServiceBusQueueConfigurator configurator);
    SendSettings GetDeadLetterSettings(IServiceBusQueueConfigurator configurator);
}
