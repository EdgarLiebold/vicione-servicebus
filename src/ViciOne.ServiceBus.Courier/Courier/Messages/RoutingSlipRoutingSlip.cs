using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes routing-slip wire data and creates isolated snapshots for locally built instances.</summary>
internal sealed class RoutingSlipRoutingSlip :
    IRoutingSlip
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipRoutingSlip()
    {
    }

    /// <summary>Creates an isolated, read-only snapshot of a routing slip.</summary>
    /// <param name="trackingNumber">The non-empty routing-slip identifier.</param>
    /// <param name="createTimestamp">The routing-slip creation timestamp.</param>
    /// <param name="activities">The remaining itinerary.</param>
    /// <param name="activityLogs">The completed activity records.</param>
    /// <param name="compensateLogs">The pending compensation records.</param>
    /// <param name="exceptions">The activity failure records.</param>
    /// <param name="variables">The routing-slip variables.</param>
    /// <param name="subscriptions">The lifecycle-event subscriptions.</param>
    public RoutingSlipRoutingSlip(Guid trackingNumber, DateTimeOffset createTimestamp, IEnumerable<IActivity> activities,
        IEnumerable<IActivityLog> activityLogs, IEnumerable<ICompensateLog> compensateLogs, IEnumerable<IActivityException> exceptions,
        IEnumerable<KeyValuePair<string, object>> variables, IEnumerable<ISubscription> subscriptions)
    {
        if (trackingNumber == Guid.Empty)
            throw new ArgumentException("The routing-slip tracking number cannot be empty.", nameof(trackingNumber));
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(activityLogs);
        ArgumentNullException.ThrowIfNull(compensateLogs);
        ArgumentNullException.ThrowIfNull(exceptions);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(subscriptions);

        TrackingNumber = trackingNumber;
        CreateTimestamp = createTimestamp;
        Itinerary = Array.AsReadOnly(activities.Select(activity => (IActivity)new RoutingSlipActivity(activity)).ToArray());
        ActivityLogs = Array.AsReadOnly(activityLogs.Select(log => (IActivityLog)new RoutingSlipActivityLog(log)).ToArray());
        CompensateLogs = Array.AsReadOnly(compensateLogs.Select(log => (ICompensateLog)new RoutingSlipCompensateLog(log)).ToArray());
        Variables = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(variables, StringComparer.OrdinalIgnoreCase));
        ActivityExceptions = Array.AsReadOnly(exceptions.Select(exception => (IActivityException)new RoutingSlipActivityException(exception)).ToArray());
        Subscriptions = Array.AsReadOnly(subscriptions.Select(subscription => (ISubscription)new RoutingSlipSubscription(subscription)).ToArray());
    }

    /// <summary>Gets or sets the routing-slip identifier.</summary>
    public Guid TrackingNumber { get; set; }
    /// <summary>Gets or sets the routing-slip creation timestamp.</summary>
    public DateTimeOffset CreateTimestamp { get; set; }
    /// <summary>Gets or sets the remaining itinerary.</summary>
    public IReadOnlyList<IActivity> Itinerary { get; set; } = null!;
    /// <summary>Gets or sets the completed activity records.</summary>
    public IReadOnlyList<IActivityLog> ActivityLogs { get; set; } = null!;
    /// <summary>Gets or sets the pending compensation records.</summary>
    public IReadOnlyList<ICompensateLog> CompensateLogs { get; set; } = null!;
    /// <summary>Gets or sets the routing-slip variables.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; set; } = null!;
    /// <summary>Gets or sets the activity failure records.</summary>
    public IReadOnlyList<IActivityException> ActivityExceptions { get; set; } = null!;
    /// <summary>Gets or sets the lifecycle-event subscriptions.</summary>
    public IReadOnlyList<ISubscription> Subscriptions { get; set; } = null!;
}
