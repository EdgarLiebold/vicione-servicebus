namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Carries a message together with the advanced send pipeline used to initialize it.</summary>
/// <typeparam name="T">The message contract type.</typeparam>
public readonly struct InitializedMessage<T>
    where T : class
{
    readonly T? _message;
    readonly IPipe<SendContext<T>>? _pipe;

    /// <summary>Initializes a message and its send-context pipeline.</summary>
    /// <param name="message">The initialized message.</param>
    /// <param name="pipe">The send-context pipeline, or <see langword="null" /> when no additional stages are required.</param>
    public InitializedMessage(T message, IPipe<SendContext<T>>? pipe)
    {
        ArgumentNullException.ThrowIfNull(message);

        _message = message;
        _pipe = pipe != null && pipe.IsNotEmpty() ? pipe : Advanced.Middleware.Pipe.Empty<SendContext<T>>();
    }

    /// <summary>Initializes a message with an empty send-context pipeline.</summary>
    /// <param name="message">The initialized message.</param>
    public InitializedMessage(T message) : this(message, null)
    {
    }

    /// <summary>Gets the initialized message.</summary>
    /// <exception cref="InvalidOperationException">The value was created through the default struct initializer.</exception>
    public T Message => _message
        ?? throw new InvalidOperationException("The initialized message value has not been constructed.");

    /// <summary>Gets the send-context pipeline associated with the initialized message.</summary>
    /// <exception cref="InvalidOperationException">The value was created through the default struct initializer.</exception>
    public IPipe<SendContext<T>> Pipe => _pipe
        ?? throw new InvalidOperationException("The initialized message value has not been constructed.");

    /// <summary>Deconstructs this value into its components.</summary>
    /// <param name="message">Receives the initialized message.</param>
    /// <param name="pipe">Receives the send-context pipeline.</param>
    public void Deconstruct(out T message, out IPipe<SendContext<T>> pipe)
    {
        message = Message;
        pipe = Pipe;
    }
}
