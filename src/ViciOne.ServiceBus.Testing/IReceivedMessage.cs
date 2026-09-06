using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by received message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReceivedMessage<out T> :
    IReceivedMessage
    where T : class
{
    /// <summary>Gets the context.</summary>
    new ConsumeContext<T> Context { get; }
}


/// <summary>Defines the operations required by received message.</summary>
public interface IReceivedMessage :
    IAsyncListElement
{
    /// <summary>Gets the context.</summary>
    ConsumeContext Context { get; }

    /// <summary>Gets the start time.</summary>
    DateTimeOffset StartTime { get; }
    /// <summary>Gets the elapsed time.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>Gets the exception.</summary>
    Exception? Exception { get; }
    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }
    /// <summary>Gets the short type name.</summary>
    string ShortTypeName { get; }
    /// <summary>Gets the message object.</summary>
    object MessageObject { get; }
}
