using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future routing slip handle.
/// </summary>
public interface FutureRoutingSlipHandle
{
    /// <summary>
    /// The fault state machine event
    /// </summary>
    Event<RoutingSlipFaulted> Faulted { get; }

    /// <summary>
    /// The response state machine event
    /// </summary>
    Event<RoutingSlipCompleted> Completed { get; }
}
