using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Carries routing slip completed message data.</summary>
internal sealed class RoutingSlipCompletedMessage :
    RoutingSlipCompleted
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipCompletedMessage()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="variables">The variables.</param>
    public RoutingSlipCompletedMessage(Guid trackingNumber, DateTimeOffset timestamp, TimeSpan duration, IDictionary<string, object> variables)
    {
        Duration = duration;
        Timestamp = timestamp;

        TrackingNumber = trackingNumber;
        Variables = variables;
    }

    /// <summary>Gets or sets the tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the duration.</summary>
    public TimeSpan Duration { get; set; }
    /// <summary>Gets or sets the variables.</summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
}
