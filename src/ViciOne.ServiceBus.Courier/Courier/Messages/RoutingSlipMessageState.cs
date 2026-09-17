using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
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
    /// <param name="values">The key/value state to copy.</param>
    /// <param name="parameterName">The originating call-site expression reported when <paramref name="values"/> is null.</param>
    /// <returns>A detached, case-insensitive, read-only dictionary.</returns>
    public static IReadOnlyDictionary<string, object> Snapshot(
        IReadOnlyDictionary<string, object> values,
        [CallerArgumentExpression(nameof(values))] string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);

        return new ReadOnlyDictionary<string, object>(
            new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Creates a read-only deep copy of routing-slip itinerary entries.</summary>
    /// <param name="activities">The itinerary entries to copy.</param>
    /// <param name="parameterName">The originating call-site expression reported for invalid collection input.</param>
    /// <returns>A detached, read-only list containing detached activity entries.</returns>
    public static IReadOnlyList<IActivity> SnapshotActivities(
        IEnumerable<IActivity> activities,
        [CallerArgumentExpression(nameof(activities))] string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(activities, parameterName);

        return Array.AsReadOnly(activities.Select(activity => activity is null
            ? throw new ArgumentException("The activity collection cannot contain null entries.", parameterName)
            : (IActivity)new RoutingSlipActivity(activity)).ToArray());
    }

    /// <summary>Creates a read-only deep copy of routing-slip activity failures.</summary>
    /// <param name="exceptions">The activity failures to copy.</param>
    /// <param name="parameterName">The originating call-site expression reported for invalid collection input.</param>
    /// <returns>A detached, read-only list containing detached activity-failure envelopes.</returns>
    public static IReadOnlyList<IActivityException> SnapshotExceptions(
        IEnumerable<IActivityException> exceptions,
        [CallerArgumentExpression(nameof(exceptions))] string? parameterName = null)
    {
        ArgumentNullException.ThrowIfNull(exceptions, parameterName);

        return Array.AsReadOnly(exceptions.Select(exception => exception is null
            ? throw new ArgumentException("The activity-exception collection cannot contain null entries.", parameterName)
            : (IActivityException)new RoutingSlipActivityException(exception)).ToArray());
    }
}
