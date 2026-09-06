using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip routing slip implementation.
/// </summary>
public class RoutingSlipRoutingSlip :
    RoutingSlip
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipRoutingSlip()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="trackingNumber">The tracking number value.</param>
    /// <param name="createTimestamp">The create timestamp value.</param>
    /// <param name="activities">The activities value.</param>
    /// <param name="activityLogs">The activity logs value.</param>
    /// <param name="compensateLogs">The compensate logs value.</param>
    /// <param name="exceptions">The exceptions value.</param>
    /// <param name="variables">The variables value.</param>
    /// <param name="subscriptions">The subscriptions value.</param>
    public RoutingSlipRoutingSlip(Guid trackingNumber, DateTimeOffset createTimestamp, IEnumerable<Activity> activities,
        IEnumerable<ActivityLog> activityLogs, IEnumerable<CompensateLog> compensateLogs, IEnumerable<ActivityException> exceptions,
        IDictionary<string, object> variables, IEnumerable<Subscription> subscriptions)
    {
        TrackingNumber = trackingNumber;
        CreateTimestamp = createTimestamp;
        Itinerary = activities.ToList();
        ActivityLogs = activityLogs.ToList();
        CompensateLogs = compensateLogs.ToList();
        Variables = variables;
        ActivityExceptions = exceptions.ToList();
        Subscriptions = subscriptions.ToList();
    }

    /// <summary>
    /// Gets or sets the tracking number value.
    /// </summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>
    /// Gets or sets the create timestamp value.
    /// </summary>
    public DateTimeOffset CreateTimestamp { get; set; }
    /// <summary>
    /// Gets or sets the itinerary value.
    /// </summary>
    public IList<Activity> Itinerary { get; set; } = null!;
    /// <summary>
    /// Gets or sets the activity logs value.
    /// </summary>
    public IList<ActivityLog> ActivityLogs { get; set; } = null!;
    /// <summary>
    /// Gets or sets the compensate logs value.
    /// </summary>
    public IList<CompensateLog> CompensateLogs { get; set; } = null!;
    /// <summary>
    /// Gets or sets the variables value.
    /// </summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>
    /// Gets or sets the activity exceptions value.
    /// </summary>
    public IList<ActivityException> ActivityExceptions { get; set; } = null!;
    /// <summary>
    /// Gets or sets the subscriptions value.
    /// </summary>
    public IList<Subscription> Subscriptions { get; set; } = null!;
}
