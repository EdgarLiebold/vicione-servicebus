namespace ViciOne.ServiceBus.MessageData.Conventions
{
    using ViciOne.ServiceBus.Configuration;


    public interface IMessageDataMessageSendTopologyConvention<TMessage> :
        IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
    }
}
