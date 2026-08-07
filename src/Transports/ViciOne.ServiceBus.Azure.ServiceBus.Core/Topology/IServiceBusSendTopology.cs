// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using AzureServiceBusTransport;


    public interface IServiceBusSendTopology :
        ISendTopology
    {
        new IServiceBusMessageSendTopology<T> GetMessageTopology<T>()
            where T : class;

        SendSettings GetSendSettings(ServiceBusEndpointAddress address);

        SendSettings GetErrorSettings(IServiceBusQueueConfigurator configurator);
        SendSettings GetDeadLetterSettings(IServiceBusQueueConfigurator configurator);
    }
}
