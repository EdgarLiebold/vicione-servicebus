using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the transport-independent execution metadata shared by activity pipelines.</summary>
public interface ActivityContext :
    ConsumeContext
{
    /// <summary>Gets the immutable routing-slip variables visible to the activity.</summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>Gets the identifier shared by every event and activity in the routing slip.</summary>
    Guid TrackingNumber { get; }

    /// <summary>Gets the itinerary name of the current activity.</summary>
    string ActivityName { get; }

    /// <summary>Gets the identifier of this activity execution attempt.</summary>
    Guid ExecutionId { get; }

    /// <summary>Gets the UTC timestamp at which this activity context was created.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the elapsed time since this activity context was created.</summary>
    TimeSpan Elapsed { get; }

    /// <summary>Notifies the receive pipeline that the activity message was consumed.</summary>
    /// <param name="duration">The time spent consuming the activity message.</param>
    /// <param name="consumerType">The diagnostic consumer identity recorded with the notification.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes after all consume observers have been notified.</returns>
    Task NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default);
}
