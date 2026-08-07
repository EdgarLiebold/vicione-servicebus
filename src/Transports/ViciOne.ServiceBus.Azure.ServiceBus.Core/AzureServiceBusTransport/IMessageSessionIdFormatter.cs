// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    public interface IMessageSessionIdFormatter<in TMessage>
        where TMessage : class
    {
        string FormatSessionId(SendContext<TMessage> context);
    }
}
