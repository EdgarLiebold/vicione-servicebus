using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes terminal routing-slip events configured for a future.</summary>
public interface IFutureRoutingSlipHandle
{
    /// <summary>Gets the event raised when the routing slip faults.</summary>
    IEvent<RoutingSlipFaulted> Faulted { get; }

    /// <summary>Gets the event raised when the routing slip completes.</summary>
    IEvent<RoutingSlipCompleted> Completed { get; }
}
