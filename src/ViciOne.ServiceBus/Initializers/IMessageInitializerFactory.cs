namespace ViciOne.ServiceBus.Initializers;

public interface IMessageInitializerFactory<TMessage>
    where TMessage : class
{
    IMessageInitializer<TMessage> CreateMessageInitializer();
}
