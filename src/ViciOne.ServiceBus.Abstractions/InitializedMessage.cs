namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>
/// Carries a message together with the advanced send pipeline used to initialize it.
/// </summary>
/// <typeparam name="T">The initialized message type.</typeparam>
public readonly struct InitializedMessage<T>
    where T : class
{
    /// <summary>
    /// Defines the message value.
    /// </summary>
    public readonly T Message;
    /// <summary>
    /// Defines the pipe value.
    /// </summary>
    public readonly IPipe<SendContext<T>> Pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    public InitializedMessage(T message, IPipe<SendContext<T>>? pipe)
    {
        Message = message;
        Pipe = pipe != null && pipe.IsNotEmpty() ? pipe : Advanced.Middleware.Pipe.Empty<SendContext<T>>();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public InitializedMessage(T message)
    {
        Message = message;
        Pipe = Advanced.Middleware.Pipe.Empty<SendContext<T>>();
    }

    /// <summary>
    /// Deconstructs this value into its components.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    public void Deconstruct(out T message, out IPipe<SendContext<T>> pipe)
    {
        message = Message;
        pipe = Pipe;
    }
}
