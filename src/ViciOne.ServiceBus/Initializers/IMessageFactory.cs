namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Creates the message type
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
public interface IMessageFactory<out TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    InitializeContext<TMessage> Create(InitializeContext context);
}


/// <summary>
/// Creates the message type
/// </summary>
public interface IMessageFactory
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    object Create();
}
