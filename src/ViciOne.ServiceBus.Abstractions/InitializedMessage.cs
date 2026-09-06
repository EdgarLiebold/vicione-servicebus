namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Carries a message together with the advanced send pipeline used to initialize it.</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct InitializedMessage<T>
    where T : class
{
    /// <summary>Exposes the message used by the containing type.</summary>
    public readonly T Message;
    /// <summary>Exposes the pipe used by the containing type.</summary>
    public readonly IPipe<SendContext<T>> Pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public InitializedMessage(T message, IPipe<SendContext<T>>? pipe)
    {
        Message = message;
        Pipe = pipe != null && pipe.IsNotEmpty() ? pipe : Advanced.Middleware.Pipe.Empty<SendContext<T>>();
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public InitializedMessage(T message)
    {
        Message = message;
        Pipe = Advanced.Middleware.Pipe.Empty<SendContext<T>>();
    }

    /// <summary>Deconstructs this value into its components.</summary>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <param name="pipe">Receives the pipe produced by the operation.</param>
    public void Deconstruct(out T message, out IPipe<SendContext<T>> pipe)
    {
        message = Message;
        pipe = Pipe;
    }
}
