namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Creates dynamic message instances.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TImplementation">The implementation type.</typeparam>
public class DynamicMessageFactory<TMessage, TImplementation> :
    IMessageFactory<TMessage>
    where TMessage : class
    where TImplementation : TMessage, new()
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        var message = new TImplementation();

        return context.CreateMessageContext<TMessage>(message);
    }
}


/// <summary>Creates dynamic message instances.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DynamicMessageFactory<TMessage> :
    IMessageFactory<TMessage>,
    IMessageFactory
    where TMessage : class, new()
{
    /// <summary>Creates the requested value.</summary>
    /// <returns>The newly created instance.</returns>
    public object Create()
    {
        return new TMessage();
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        var message = new TMessage();

        return context.CreateMessageContext(message);
    }
}
