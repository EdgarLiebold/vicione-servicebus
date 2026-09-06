using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes the transport-independent execution metadata shared by activity pipelines.</summary>
public interface ActivityContext :
    ConsumeContext
{
    /// <summary>Gets the variables.</summary>
    IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>Gets the tracking number.</summary>
    Guid TrackingNumber { get; }

    /// <summary>Gets the activity name.</summary>
    string ActivityName { get; }

    /// <summary>Gets the execution id.</summary>
    Guid ExecutionId { get; }

    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the elapsed.</summary>
    TimeSpan Elapsed { get; }

    /// <summary>Notifies the receive pipeline that the activity message was consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the notification.</returns>
    Task NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default);
}
