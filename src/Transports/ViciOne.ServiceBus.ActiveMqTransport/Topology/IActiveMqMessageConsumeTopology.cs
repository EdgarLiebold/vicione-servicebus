namespace ViciOne.ServiceBus
{
    public interface IActiveMqMessageConsumeTopology<TMessage> :
        IMessageConsumeTopology<TMessage>
        where TMessage : class
    {
    }
}
