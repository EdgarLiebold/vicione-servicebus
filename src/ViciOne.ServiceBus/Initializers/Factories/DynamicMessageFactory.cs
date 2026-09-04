namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>
/// Provides a dynamic message factory implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TImplementation">The t implementation type.</typeparam>
public class DynamicMessageFactory<TMessage, TImplementation> :
    IMessageFactory<TMessage>
    where TMessage : class
    where TImplementation : TMessage, new()
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        var message = new TImplementation();

        return context.CreateMessageContext<TMessage>(message);
    }
}


/// <summary>
/// Provides a dynamic message factory implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DynamicMessageFactory<TMessage> :
    IMessageFactory<TMessage>,
    IMessageFactory
    where TMessage : class, new()
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public object Create()
    {
        return new TMessage();
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        var message = new TMessage();

        return context.CreateMessageContext(message);
    }
}
