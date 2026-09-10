using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes a recorded consumption of a strongly typed message.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface IConsumedMessage<out TMessage> :
    IConsumedMessage
    where TMessage : class
{
    /// <summary>Gets the consume context that was observed.</summary>
    new ConsumeContext<TMessage> Context { get; }
}


/// <summary>Describes a recorded message-consumption attempt.</summary>
public interface IConsumedMessage :
    IAsyncListElement
{
    /// <summary>Gets the consume context that was observed.</summary>
    ConsumeContext Context { get; }

    /// <summary>Gets the estimated time at which message processing began.</summary>
    DateTimeOffset StartTime { get; }
    /// <summary>Gets the elapsed receive-pipeline time recorded by the transport.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>Gets the consume-pipeline exception, or <see langword="null"/> for a successful attempt.</summary>
    Exception? Exception { get; }
    /// <summary>Gets the declared message contract type.</summary>
    Type MessageType { get; }
    /// <summary>Gets the service-bus short name of the message contract.</summary>
    string ShortTypeName { get; }
    /// <summary>Gets the consumed message instance.</summary>
    object MessageObject { get; }
}
