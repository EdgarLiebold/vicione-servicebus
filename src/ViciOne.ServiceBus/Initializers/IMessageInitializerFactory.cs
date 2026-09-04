namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Defines the contract for message initializer factory.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageInitializerFactory<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Creates message initializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IMessageInitializer<TMessage> CreateMessageInitializer();
}
