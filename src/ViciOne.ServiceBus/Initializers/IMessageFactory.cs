namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates the message type.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IMessageFactory<out TMessage>
    where TMessage : class
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    InitializeContext<TMessage> Create(InitializeContext context);
}


/// <summary>Creates the message type.</summary>
public interface IMessageFactory
{
    /// <summary>Creates the requested value.</summary>
    /// <returns>The newly created instance.</returns>
    object Create();
}
