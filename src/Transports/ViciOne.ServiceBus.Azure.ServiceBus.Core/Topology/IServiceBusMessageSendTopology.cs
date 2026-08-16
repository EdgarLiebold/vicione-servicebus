namespace ViciOne.ServiceBus
{
    public interface IServiceBusMessageSendTopology<TMessage> :
        IMessageSendTopology<TMessage>
        where TMessage : class
    {
    }
}
