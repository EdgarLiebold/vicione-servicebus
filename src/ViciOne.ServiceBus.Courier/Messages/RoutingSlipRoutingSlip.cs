using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Represents the mutable wire contract of a routing slip.</summary>
internal sealed class RoutingSlipRoutingSlip :
    RoutingSlip
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipRoutingSlip()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="createTimestamp">The create timestamp.</param>
    /// <param name="activities">The activities.</param>
    /// <param name="activityLogs">The activity logs.</param>
    /// <param name="compensateLogs">The compensate logs.</param>
    /// <param name="exceptions">The exceptions.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="subscriptions">The subscriptions.</param>
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

    /// <summary>Gets or sets the tracking number.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the create timestamp.</summary>
    public DateTimeOffset CreateTimestamp { get; set; }
    /// <summary>Gets or sets the itinerary.</summary>
    public IList<Activity> Itinerary { get; set; } = null!;
    /// <summary>Gets or sets the activity logs.</summary>
    public IList<ActivityLog> ActivityLogs { get; set; } = null!;
    /// <summary>Gets or sets the compensate logs.</summary>
    public IList<CompensateLog> CompensateLogs { get; set; } = null!;
    /// <summary>Gets or sets the variables.</summary>
    public IDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the activity exceptions.</summary>
    public IList<ActivityException> ActivityExceptions { get; set; } = null!;
    /// <summary>Gets or sets the subscriptions.</summary>
    public IList<Subscription> Subscriptions { get; set; } = null!;
}
