using System;

namespace ViciOne.ServiceBus;

/// <summary>Describes a message accepted for future delivery.</summary>
public interface ScheduledMessage
{
    /// <summary>Gets the token that identifies the scheduled message.</summary>
    Guid TokenId { get; }
    /// <summary>Gets the time at which the message becomes eligible for delivery.</summary>
    DateTimeOffset DueAt { get; }
    /// <summary>Gets the message destination.</summary>
    Uri Destination { get; }
}

/// <summary>Describes a typed message accepted for future delivery.</summary>
/// <typeparam name="T">The scheduled message contract.</typeparam>
public interface ScheduledMessage<out T> :
    ScheduledMessage
    where T : class
{
    /// <summary>Gets the original message payload.</summary>
    T Payload { get; }
}
