using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for courier context.
/// </summary>
public interface CourierContext :
    ConsumeContext<RoutingSlip>,
    ConsumeContext
{
    /// <summary>
    /// The tracking number for this routing slip
    /// </summary>
    Guid TrackingNumber { get; }

    /// <summary>
    /// The name of the activity
    /// </summary>
    string ActivityName { get; }

    /// <summary>
    /// The executionId for this attempt at the activity
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// The start time for the activity execution
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The time elapsed for the execution operation
    /// </summary>
    TimeSpan Elapsed { get; }
}
