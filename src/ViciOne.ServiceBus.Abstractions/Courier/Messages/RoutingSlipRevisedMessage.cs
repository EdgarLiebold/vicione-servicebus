using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip revised message implementation.
/// </summary>
[Serializable]
public class RoutingSlipRevisedMessage :
    RoutingSlipRevised
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipRevisedMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="itinerary">The itinerary value.</param>
    /// <param name="discardedItinerary">The discarded itinerary value.</param>
    public RoutingSlipRevisedMessage(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, DateTimeOffset timestamp, TimeSpan duration,
        IDictionary<string, object> variables,
        IEnumerable<Activity> itinerary, IEnumerable<Activity> discardedItinerary)
    {
        Host = host;
        ActivityName = activityName;
        TrackingNumber = trackingNumber;
        Timestamp = timestamp;
        Duration = duration;
        ExecutionId = executionId;
        Variables = variables;
        Itinerary = itinerary.ToArray();
        DiscardedItinerary = discardedItinerary.ToArray();
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
    /// Gets or sets the activity name value.
    /// </summary>
    public string ActivityName { get; set; } = null!;
    /// <summary>
    /// Gets or sets the execution id value.
    /// </summary>
    public Guid ExecutionId { get; set; }
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>
    /// Gets or sets the itinerary value.
    /// </summary>
    public Activity[] Itinerary { get; set; } = null!;
    /// <summary>
    /// Gets or sets the discarded itinerary value.
    /// </summary>
    public Activity[] DiscardedItinerary { get; set; } = null!;
}
