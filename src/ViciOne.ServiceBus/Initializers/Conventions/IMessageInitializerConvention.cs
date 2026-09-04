namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>
/// Defines the contract for message input initializer convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageInputInitializerConvention<in TMessage>
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for message initializer convention.
/// </summary>
public interface IMessageInitializerConvention
{
}
