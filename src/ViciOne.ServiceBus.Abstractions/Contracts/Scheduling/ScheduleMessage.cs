using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Defines the contract for schedule message.
/// </summary>
public interface ScheduleMessage
{
    /// <summary>
    /// Gets the token id value.
    /// </summary>
    Guid TokenId { get; }

    /// <summary>
    /// The time at which the message should be published, should be in UTC
    /// </summary>
    DateTimeOffset DueAt { get; }

    /// <summary>
    /// The message types implemented by the message
    /// </summary>
    string[] PayloadType { get; }

    /// <summary>
    /// The destination where the message should be sent
    /// </summary>
    Uri Destination { get; }

    /// <summary>
    /// The actual message payload to deliver
    /// </summary>
    object Payload { get; }
}
