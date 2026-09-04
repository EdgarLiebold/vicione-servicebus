using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip completed message implementation.
/// </summary>
[Serializable]
public class RoutingSlipCompletedMessage :
    RoutingSlipCompleted
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipCompletedMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    public RoutingSlipCompletedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables)
    {
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        Variables = variables;
    }

    /// <summary>
    /// Gets or sets the tracking number value.
    /// </summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the duration value.
    /// </summary>
    public TimeSpan Duration { get; set; }
    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
}
