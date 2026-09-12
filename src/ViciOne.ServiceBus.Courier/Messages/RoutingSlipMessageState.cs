using System.Collections.ObjectModel;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Validates locally created lifecycle events and detaches their collection state from callers.</summary>
internal static class RoutingSlipMessageState
{
    /// <summary>Validates fields shared by all routing-slip lifecycle events.</summary>
    public static void Validate(Guid trackingNumber, TimeSpan duration)
    {
        if (trackingNumber == Guid.Empty)
            throw new ArgumentException("The routing-slip tracking number cannot be empty.", nameof(trackingNumber));

        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
    }

    /// <summary>Validates fields shared by activity-level routing-slip lifecycle events.</summary>
    public static void ValidateActivity(HostInfo host, Guid trackingNumber, string activityName, Guid executionId, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(host);
        Validate(trackingNumber, duration);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        if (executionId == Guid.Empty)
            throw new ArgumentException("The activity execution identifier cannot be empty.", nameof(executionId));
    }

    /// <summary>Creates a case-insensitive read-only copy of routing-slip key/value state.</summary>
    public static IReadOnlyDictionary<string, object> Snapshot(IReadOnlyDictionary<string, object> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return new ReadOnlyDictionary<string, object>(
            new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Creates a read-only deep copy of routing-slip itinerary entries.</summary>
    public static IReadOnlyList<Activity> SnapshotActivities(IEnumerable<Activity> activities)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return Array.AsReadOnly(activities.Select(activity => (Activity)new RoutingSlipActivity(activity)).ToArray());
    }

    /// <summary>Creates a read-only deep copy of routing-slip activity failures.</summary>
    public static IReadOnlyList<ActivityException> SnapshotExceptions(IEnumerable<ActivityException> exceptions)
    {
        ArgumentNullException.ThrowIfNull(exceptions);

        return Array.AsReadOnly(exceptions.Select(exception => (ActivityException)new RoutingSlipActivityException(exception)).ToArray());
    }
}
