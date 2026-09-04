using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for received message.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IReceivedMessage<out T> :
    IReceivedMessage
    where T : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    new ConsumeContext<T> Context { get; }
}


/// <summary>
/// Defines the contract for received message.
/// </summary>
public interface IReceivedMessage :
    IAsyncListElement
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ConsumeContext Context { get; }

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
