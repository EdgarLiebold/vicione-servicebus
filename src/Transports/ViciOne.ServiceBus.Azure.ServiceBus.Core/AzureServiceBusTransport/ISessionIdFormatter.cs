// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    public interface ISessionIdFormatter
    {
        string FormatSessionId<T>(SendContext<T> context)
            where T : class;
    }
}
