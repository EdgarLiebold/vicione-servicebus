using System;

namespace ViciOne.ServiceBus.Scheduling;

public interface ScheduleMessage
{
    Guid TokenId { get; }

    /// <summary>
    /// The time at which the message should be published, should be in UTC
    /// </summary>
    DateTime ScheduledTime { get; }

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
