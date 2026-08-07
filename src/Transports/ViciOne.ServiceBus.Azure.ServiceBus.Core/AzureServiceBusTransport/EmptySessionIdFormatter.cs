// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    public class EmptySessionIdFormatter :
        ISessionIdFormatter
    {
        string ISessionIdFormatter.FormatSessionId<T>(SendContext<T> context)
        {
            return null;
        }
    }
}
