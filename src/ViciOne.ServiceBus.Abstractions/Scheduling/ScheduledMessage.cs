using System;

namespace ViciOne.ServiceBus;

/// <summary>Defines the operations required by scheduled message.</summary>
public interface ScheduledMessage
{
    /// <summary>Gets the token id.</summary>
    Guid TokenId { get; }
    /// <summary>Gets the due at.</summary>
    DateTimeOffset DueAt { get; }
    /// <summary>Gets the destination.</summary>
    Uri Destination { get; }
}


/// <summary>Defines the operations required by scheduled message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ScheduledMessage<out T> :
    ScheduledMessage
    where T : class
{
    /// <summary>Gets the payload.</summary>
    T Payload { get; }
}
