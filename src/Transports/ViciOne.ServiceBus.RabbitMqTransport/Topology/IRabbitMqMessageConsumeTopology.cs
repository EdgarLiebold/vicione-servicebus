namespace ViciOne.ServiceBus
{
    public interface IRabbitMqMessageConsumeTopology<TMessage> :
        IMessageConsumeTopology<TMessage>
        where TMessage : class
    {
    }
}
