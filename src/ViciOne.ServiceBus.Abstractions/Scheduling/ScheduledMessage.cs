using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Defines the contract for scheduled message.
/// </summary>
public interface ScheduledMessage
{
    /// <summary>
    /// Gets the token id value.
    /// </summary>
    Guid TokenId { get; }
    /// <summary>
    /// Gets the due at value.
    /// </summary>
    DateTimeOffset DueAt { get; }
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    Uri Destination { get; }
}


/// <summary>
/// Defines the contract for scheduled message.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ScheduledMessage<out T> :
    ScheduledMessage
    where T : class
{
    /// <summary>
    /// Gets the payload value.
    /// </summary>
    T Payload { get; }
}
