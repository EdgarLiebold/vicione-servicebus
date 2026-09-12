using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates an isolated, non-null routing-slip representation from received contract data.</summary>
internal sealed class SanitizedRoutingSlip :
    RoutingSlip
{
    readonly SerializerContext _serializerContext;

    /// <summary>Copies received routing-slip state into validated, case-insensitive, caller-independent collections.</summary>
    /// <param name="context">The received routing-slip context and serializer boundary.</param>
    public SanitizedRoutingSlip(ConsumeContext<RoutingSlip> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _serializerContext = context.Advanced().SerializerContext;

        var routingSlip = context.Message;
        if (routingSlip.TrackingNumber == Guid.Empty)
            throw new SerializationException("A routing slip requires a non-empty tracking number.");
        if (routingSlip.CreateTimestamp == default)
            throw new SerializationException("A routing slip requires a creation timestamp.");

        TrackingNumber = routingSlip.TrackingNumber;
        CreateTimestamp = routingSlip.CreateTimestamp;

        Itinerary = (routingSlip.Itinerary ?? [])
            .Select(Activity (x) => new RoutingSlipActivity(x))
            .ToArray();

        ActivityLogs = (routingSlip.ActivityLogs ?? [])
            .Select(ActivityLog (x) => new RoutingSlipActivityLog(x))
            .ToArray();

        CompensateLogs = (routingSlip.CompensateLogs ?? [])
            .Select(CompensateLog (x) => new RoutingSlipCompensateLog(x))
            .ToArray();

        Variables = new ReadOnlyDictionary<string, object>(
            new Dictionary<string, object>(routingSlip.Variables ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase));

        ActivityExceptions = (routingSlip.ActivityExceptions ?? [])
            .Select(ActivityException (x) => new RoutingSlipActivityException(x))
            .ToArray();

        Subscriptions = (routingSlip.Subscriptions ?? [])
            .Select(Subscription (x) => new RoutingSlipSubscription(x))
            .ToArray();
    }

    /// <summary>Gets the tracking number.</summary>
    public Guid TrackingNumber { get; }
    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreateTimestamp { get; }
    /// <summary>Gets the remaining itinerary.</summary>
    public IReadOnlyList<Activity> Itinerary { get; }
    /// <summary>Gets the completed activity logs.</summary>
    public IReadOnlyList<ActivityLog> ActivityLogs { get; }
    /// <summary>Gets the pending compensation logs.</summary>
    public IReadOnlyList<CompensateLog> CompensateLogs { get; }
    /// <summary>Gets the routing-slip variables.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; }
    /// <summary>Gets the recorded activity exceptions.</summary>
    public IReadOnlyList<ActivityException> ActivityExceptions { get; }
    /// <summary>Gets the event subscriptions.</summary>
    public IReadOnlyList<Subscription> Subscriptions { get; }

    /// <summary>Combines routing-slip variables with the next activity's values and deserializes its arguments.</summary>
    /// <typeparam name="T">The activity-arguments contract.</typeparam>
    /// <returns>The deserialized arguments for the next itinerary entry.</returns>
    public T GetActivityArguments<T>()
        where T : class
    {
        try
        {
            if (Itinerary.Count == 0)
                throw new RoutingSlipArgumentException("The routing slip does not contain an activity.");

            var activity = Itinerary[0];

            IReadOnlyDictionary<string, object> argumentsDictionary = Variables.Count > 0
                ? Merge(Variables, activity.Arguments)
                : activity.Arguments;

            return _serializerContext.DeserializeObject<T>(argumentsDictionary)
                ?? throw new RoutingSlipArgumentException($"The activity arguments could not be deserialized as {TypeCache<T>.ShortName}.");
        }
        catch (RoutingSlipArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RoutingSlipArgumentException("The activity arguments could not be read.", ex);
        }
    }

    /// <summary>Combines routing-slip variables with the newest compensation entry and deserializes its log.</summary>
    /// <typeparam name="T">The compensation-log contract.</typeparam>
    /// <returns>The deserialized log for the activity being compensated.</returns>
    public T GetCompensateLogData<T>()
        where T : class
    {
        try
        {
            if (CompensateLogs.Count == 0)
                throw new RoutingSlipArgumentException("The routing slip does not contain a compensation log.");

            var compensateLog = CompensateLogs[CompensateLogs.Count - 1];

            IReadOnlyDictionary<string, object> argumentsDictionary = Variables.Count > 0
                ? Merge(Variables, compensateLog.Data)
                : compensateLog.Data;

            return _serializerContext.DeserializeObject<T>(argumentsDictionary)
                ?? throw new RoutingSlipArgumentException($"The compensation log could not be deserialized as {TypeCache<T>.ShortName}.");
        }
        catch (RoutingSlipArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RoutingSlipArgumentException("The compensation log could not be read.", ex);
        }
    }

    static IReadOnlyDictionary<string, object> Merge(
        IReadOnlyDictionary<string, object> variables,
        IReadOnlyDictionary<string, object> activityValues)
    {
        var merged = new Dictionary<string, object>(variables, StringComparer.OrdinalIgnoreCase);
        foreach ((string key, object? value) in activityValues)
        {
            if (value is not null || !merged.ContainsKey(key))
                merged[key] = value!;
        }

        return merged;
    }
}
