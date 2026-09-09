namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides pipeline and message metadata to a message transformation.</summary>
public interface TransformContext :
    PipeContext,
    MessageContext
{
}


/// <summary>Provides the optional typed input of a message transformation.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface TransformContext<out TMessage> :
    TransformContext
    where TMessage : class
{
    /// <summary>Gets whether an input message is available.</summary>
    bool HasInput { get; }

    /// <summary>Gets the input message.</summary>
    TMessage Input { get; }
}
