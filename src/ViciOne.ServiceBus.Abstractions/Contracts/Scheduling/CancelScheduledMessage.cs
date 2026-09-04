using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Defines the contract for cancel scheduled message.
/// </summary>
public interface CancelScheduledMessage
{
    /// <summary>
    /// The date/time this message was created
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The token of the scheduled message
    /// </summary>
    Guid TokenId { get; }
}
