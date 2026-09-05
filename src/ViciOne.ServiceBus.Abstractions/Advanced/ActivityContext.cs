using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Exposes the transport-independent execution metadata shared by activity pipelines.
/// </summary>
public interface ActivityContext :
    ConsumeContext
{
    /// <summary>
    /// Gets the immutable view of variables carried by the current activity pipeline.
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>
    /// Gets the routing-slip tracking number.
    /// </summary>
    Guid TrackingNumber { get; }

    /// <summary>
    /// Gets the configured activity name.
    /// </summary>
    string ActivityName { get; }

    /// <summary>
    /// Gets the identifier of the current execution attempt.
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// Gets the time at which the current execution attempt started.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the elapsed duration of the current execution attempt.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Notifies the receive pipeline that the activity message was consumed.
    /// </summary>
    /// <param name="duration">The activity duration.</param>
    /// <param name="consumerType">The activity consumer identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the notification.</returns>
    Task NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default);
}
