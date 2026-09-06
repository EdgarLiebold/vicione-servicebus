using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by sent message.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISentMessage<out TMessage> :
    ISentMessage
    where TMessage : class
{
    /// <summary>Gets the context.</summary>
    new SendContext<TMessage> Context { get; }
}


/// <summary>Defines the operations required by sent message.</summary>
public interface ISentMessage :
    IAsyncListElement
{
    /// <summary>Gets the context.</summary>
    SendContext Context { get; }

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
