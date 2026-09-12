namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates the cached initializer for one message contract.</summary>
internal interface IMessageInitializerFactory<TMessage>
    where TMessage : class
{
    IMessageInitializer<TMessage> CreateMessageInitializer();
}
