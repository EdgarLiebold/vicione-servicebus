using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for sent message.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISentMessage<out TMessage> :
    ISentMessage
    where TMessage : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    new SendContext<TMessage> Context { get; }
}


/// <summary>
/// Defines the contract for sent message.
/// </summary>
public interface ISentMessage :
    IAsyncListElement
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    SendContext Context { get; }

    /// <summary>
    /// Gets the start time value.
    /// </summary>
    DateTimeOffset StartTime { get; }
    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the exception value.
    /// </summary>
    Exception? Exception { get; }
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }
    /// <summary>
    /// Gets the short type name value.
    /// </summary>
    string ShortTypeName { get; }
    /// <summary>
    /// Gets the message object value.
    /// </summary>
    object MessageObject { get; }
}
