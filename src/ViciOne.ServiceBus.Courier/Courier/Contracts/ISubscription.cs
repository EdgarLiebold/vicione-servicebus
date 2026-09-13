using System;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Describes a destination and selection policy for routing-slip lifecycle events.</summary>
public interface ISubscription
{
    /// <summary>Gets the destination that receives matching events.</summary>
    Uri Address { get; }

    /// <summary>Gets the lifecycle events that trigger delivery.</summary>
    RoutingSlipEvents Events { get; }

    /// <summary>Gets the optional routing-slip data included in each delivered event.</summary>
    RoutingSlipEventContents Include { get; }

    /// <summary>Gets the activity-name filter, when delivery is limited to one activity.</summary>
    string? ActivityName { get; }

    /// <summary>Gets the custom subscription message, when configured.</summary>
    MessageEnvelope? Message { get; }
}
