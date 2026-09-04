using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipRoutingSlip :
    RoutingSlip
{
    public RoutingSlipRoutingSlip()
    {
    }

    public RoutingSlipRoutingSlip(Guid trackingNumber, DateTime createTimestamp, IEnumerable<Activity> activities,
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

    public Guid TrackingNumber { get; set; }
    public DateTime CreateTimestamp { get; set; }
    public IList<Activity> Itinerary { get; set; } = null!;
    public IList<ActivityLog> ActivityLogs { get; set; } = null!;
    public IList<CompensateLog> CompensateLogs { get; set; } = null!;
    public IDictionary<string, object> Variables { get; set; } = null!;
    public IList<ActivityException> ActivityExceptions { get; set; } = null!;
    public IList<Subscription> Subscriptions { get; set; } = null!;
}
