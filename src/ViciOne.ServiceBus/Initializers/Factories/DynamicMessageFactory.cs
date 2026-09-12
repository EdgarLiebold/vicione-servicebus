namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Creates a concrete message implementation for an interface or base contract.</summary>
internal sealed class DynamicMessageFactory<TMessage, TImplementation> :
    IMessageFactory<TMessage>
    where TMessage : class
    where TImplementation : TMessage, new()
{
    /// <summary>Creates a new implementation instance within the supplied initialization graph.</summary>
    /// <param name="context">The parent initialization context.</param>
    /// <returns>A typed child context containing the new message.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = new TImplementation();

        return context.CreateMessageContext<TMessage>(message);
    }
}


/// <summary>Creates a message contract that has a public parameterless constructor.</summary>
internal sealed class DynamicMessageFactory<TMessage> :
    IMessageFactory<TMessage>,
    IMessageFactory
    where TMessage : class, new()
{
    /// <summary>Creates a standalone message instance.</summary>
    /// <returns>A new instance of <typeparamref name="TMessage" />.</returns>
    public object Create()
    {
        return new TMessage();
    }

    /// <summary>Creates a new message instance within the supplied initialization graph.</summary>
    /// <param name="context">The parent initialization context.</param>
    /// <returns>A typed child context containing the new message.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = new TMessage();

        return context.CreateMessageContext(message);
    }
}
