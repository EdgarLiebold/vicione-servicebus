namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates message initializer instances.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageInitializerFactory<TMessage>
    where TMessage : class
{
    /// <summary>Creates message initializer.</summary>
    /// <returns>The created message initializer.</returns>
    IMessageInitializer<TMessage> CreateMessageInitializer();
}
