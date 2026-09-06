using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Controls the lifetime of future routing slip.</summary>
public interface FutureRoutingSlipHandle
{
    /// <summary>The fault state machine event.</summary>
    Event<RoutingSlipFaulted> Faulted { get; }

    /// <summary>The response state machine event.</summary>
    Event<RoutingSlipCompleted> Completed { get; }
}
