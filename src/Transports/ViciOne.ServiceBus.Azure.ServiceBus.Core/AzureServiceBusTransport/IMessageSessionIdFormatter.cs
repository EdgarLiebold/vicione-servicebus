namespace ViciOne.ServiceBus.AzureServiceBusTransport
{
    public interface IMessageSessionIdFormatter<in TMessage>
        where TMessage : class
    {
        string FormatSessionId(SendContext<TMessage> context);
    }
}
