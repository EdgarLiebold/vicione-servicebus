using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes a recorded message-publication attempt.</summary>
public interface IPublishedMessage :
    IAsyncListElement
{
    /// <summary>Gets the publish context viewed through its send-context contract.</summary>
    SendContext Context { get; }

    /// <summary>Gets the publication timestamp supplied by the context, or the observation timestamp when absent.</summary>
    DateTimeOffset StartTime { get; }
    /// <summary>Gets the elapsed time between the publication timestamp and observation.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>Gets the publish-pipeline exception, or <see langword="null"/> for a successful attempt.</summary>
    Exception? Exception { get; }
    /// <summary>Gets the declared message contract type.</summary>
    Type MessageType { get; }
    /// <summary>Gets the service-bus short name of the message contract.</summary>
    string ShortTypeName { get; }
    /// <summary>Gets the published message instance.</summary>
    object MessageObject { get; }
}


/// <summary>Describes a recorded publication of a strongly typed message.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public interface IPublishedMessage<out TMessage> :
    IPublishedMessage
    where TMessage : class
{
    /// <summary>Gets the publish context that was observed.</summary>
    new PublishContext<TMessage> Context { get; }
}
