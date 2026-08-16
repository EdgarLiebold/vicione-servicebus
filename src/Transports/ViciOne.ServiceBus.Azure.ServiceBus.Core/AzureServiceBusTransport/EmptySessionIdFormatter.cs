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
