using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip faulted message implementation.
/// </summary>
[Serializable]
public class RoutingSlipFaultedMessage :
    RoutingSlipFaulted
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipFaultedMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="activityExceptions">The activity exceptions value.</param>
    /// <param name="variables">The variables value.</param>
    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration,
        IEnumerable<ActivityException> activityExceptions, IDictionary<string, object> variables)
    {
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;

        ActivityExceptions = activityExceptions.ToArray();
        Variables = variables;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="activityException">The activity exception value.</param>
    public RoutingSlipFaultedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, ActivityException activityException)
    {
        Timestamp = timestamp;
        Duration = duration;

        TrackingNumber = trackingNumber;
        ActivityExceptions = new[] { activityException };
        Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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
    /// Gets or sets the activity exceptions value.
    /// </summary>
    public ActivityException[] ActivityExceptions { get; set; } = null!;
    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
}
