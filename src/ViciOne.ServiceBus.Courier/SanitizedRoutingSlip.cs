using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates an isolated, non-null routing-slip representation from received contract data.</summary>
internal sealed class SanitizedRoutingSlip :
    RoutingSlip
{
    readonly SerializerContext _serializerContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public SanitizedRoutingSlip(ConsumeContext<RoutingSlip> context)
    {
        _serializerContext = context.Advanced().SerializerContext;

        var routingSlip = context.Message;

        TrackingNumber = routingSlip.TrackingNumber;
        CreateTimestamp = routingSlip.CreateTimestamp;

        Itinerary = (routingSlip.Itinerary ?? [])
            .Select(Activity (x) => new RoutingSlipActivity(x))
            .ToList();

        ActivityLogs = (routingSlip.ActivityLogs ?? [])
            .Select(ActivityLog (x) => new RoutingSlipActivityLog(x))
            .ToList();

        CompensateLogs = (routingSlip.CompensateLogs ?? [])
            .Select(CompensateLog (x) => new RoutingSlipCompensateLog(x))
            .ToList();

        Variables = new Dictionary<string, object>(routingSlip.Variables ?? new Dictionary<string, object>(),
            StringComparer.OrdinalIgnoreCase);

        ActivityExceptions = (routingSlip.ActivityExceptions ?? [])
            .Select(ActivityException (x) => new RoutingSlipActivityException(x))
            .ToList();

        Subscriptions = (routingSlip.Subscriptions ?? [])
            .Select(Subscription (x) => new RoutingSlipSubscription(x))
            .ToList();
    }

    /// <summary>Gets the tracking number.</summary>
    public Guid TrackingNumber { get; }
    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreateTimestamp { get; }
    /// <summary>Gets the remaining itinerary.</summary>
    public IList<Activity> Itinerary { get; }
    /// <summary>Gets the completed activity logs.</summary>
    public IList<ActivityLog> ActivityLogs { get; }
    /// <summary>Gets the pending compensation logs.</summary>
    public IList<CompensateLog> CompensateLogs { get; }
    /// <summary>Gets the routing-slip variables.</summary>
    public IDictionary<string, object> Variables { get; }
    /// <summary>Gets the recorded activity exceptions.</summary>
    public IList<ActivityException> ActivityExceptions { get; }
    /// <summary>Gets the event subscriptions.</summary>
    public IList<Subscription> Subscriptions { get; }

    /// <summary>Gets activity arguments.</summary>
    /// <typeparam name="T">The argument type.</typeparam>
    /// <returns>The activity arguments.</returns>
    public T GetActivityArguments<T>()
        where T : class
    {
        try
        {
            if (Itinerary.Count == 0)
                throw new RoutingSlipArgumentException("The routing slip does not contain an activity.");

            var activity = Itinerary[0];

            IDictionary<string, object> argumentsDictionary = Variables.Count > 0
                ? Variables.MergeLeft(activity.Arguments)
                : activity.Arguments;

            return _serializerContext.DeserializeObject<T>(argumentsDictionary)
                ?? throw new RoutingSlipArgumentException($"The activity arguments could not be deserialized as {TypeCache<T>.ShortName}.");
        }
        catch (Exception ex)
        {
            throw new RoutingSlipArgumentException("The activity arguments could not be read.", ex);
        }
    }

    /// <summary>Gets compensate log data.</summary>
    /// <typeparam name="T">The compensation-log type.</typeparam>
    /// <returns>The compensate log data.</returns>
    public T GetCompensateLogData<T>()
        where T : class
    {
        try
        {
            if (CompensateLogs.Count == 0)
                throw new RoutingSlipArgumentException("The routing slip does not contain a compensation log.");

            var compensateLog = CompensateLogs[CompensateLogs.Count - 1];

            IDictionary<string, object> argumentsDictionary = Variables.Count > 0
                ? Variables.MergeLeft(compensateLog.Data)
                : compensateLog.Data;

            return _serializerContext.DeserializeObject<T>(argumentsDictionary)
                ?? throw new RoutingSlipArgumentException($"The compensation log could not be deserialized as {TypeCache<T>.ShortName}.");
        }
        catch (Exception ex)
        {
            throw new RoutingSlipArgumentException("The compensation log could not be read.", ex);
        }
    }
}
