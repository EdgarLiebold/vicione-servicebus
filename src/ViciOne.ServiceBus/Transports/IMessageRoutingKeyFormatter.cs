namespace ViciOne.ServiceBus.Transports
{
    public interface IMessageRoutingKeyFormatter<in TMessage>
        where TMessage : class
    {
        string FormatRoutingKey(SendContext<TMessage> context);
    }
}
