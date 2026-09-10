using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes a recorded send of a strongly typed message.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISentMessage<out TMessage> :
    ISentMessage
    where TMessage : class
{
    /// <summary>Gets the send context that was observed.</summary>
    new SendContext<TMessage> Context { get; }
}


/// <summary>Describes a recorded message-send attempt.</summary>
public interface ISentMessage :
    IAsyncListElement
{
    /// <summary>Gets the send context that was observed.</summary>
    SendContext Context { get; }

    /// <summary>Gets the send timestamp supplied by the context, or the observation timestamp when absent.</summary>
    DateTimeOffset StartTime { get; }
    /// <summary>Gets the elapsed time between the send timestamp and observation.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>Gets the send-pipeline exception, or <see langword="null"/> for a successful attempt.</summary>
    Exception? Exception { get; }
    /// <summary>Gets the declared message contract type.</summary>
    Type MessageType { get; }
    /// <summary>Gets the service-bus short name of the message contract.</summary>
    string ShortTypeName { get; }
    /// <summary>Gets the sent message instance.</summary>
    object MessageObject { get; }
}
