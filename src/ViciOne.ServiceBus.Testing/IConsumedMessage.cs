using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by consumed message.</summary>
public interface IConsumedMessage
{
    /// <summary>Gets the context.</summary>
    ConsumeContext Context { get; }

    /// <summary>Gets the exception.</summary>
    Exception Exception { get; }

    /// <summary>Gets the message type.</summary>
    Type MessageType { get; }
}


/// <summary>Defines the operations required by consumed message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IConsumedMessage<out T> :
    IConsumedMessage
    where T : class
{
    /// <summary>Gets the context.</summary>
    new ConsumeContext<T> Context { get; }
}
