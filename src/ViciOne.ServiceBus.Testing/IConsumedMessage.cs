using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for consumed message.
/// </summary>
public interface IConsumedMessage
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ConsumeContext Context { get; }

    /// <summary>
    /// Gets the exception value.
    /// </summary>
    Exception Exception { get; }

    /// <summary>
    /// Gets the message type value.
    /// </summary>
    Type MessageType { get; }
}


/// <summary>
/// Defines the contract for consumed message.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IConsumedMessage<out T> :
    IConsumedMessage
    where T : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    new ConsumeContext<T> Context { get; }
}
