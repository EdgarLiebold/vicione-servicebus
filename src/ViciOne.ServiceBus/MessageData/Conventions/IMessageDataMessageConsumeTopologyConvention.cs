namespace ViciOne.ServiceBus.MessageData.Conventions
{
    using ViciOne.ServiceBus.Configuration;


    public interface IMessageDataMessageConsumeTopologyConvention<TMessage> :
        IMessageConsumeTopologyConvention<TMessage>
        where TMessage : class
    {
    }
}
