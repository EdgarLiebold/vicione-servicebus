using System;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Accepts custom subscription messages captured through an <see cref="ISendEndpoint" /> pipeline.</summary>
internal interface IRoutingSlipSubscriptionTarget
{
    /// <summary>Adds a serialized custom message to a routing-slip lifecycle-event subscription.</summary>
    /// <param name="address">The destination that receives matching events.</param>
    /// <param name="events">The non-empty lifecycle-event selection.</param>
    /// <param name="contents">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The activity-name filter, when configured.</param>
    /// <param name="message">The serialized custom subscription message.</param>
    void AddSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents contents, string? activityName, MessageEnvelope message);
}
