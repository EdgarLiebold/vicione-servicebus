using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Represents a subscription to routing slip.</summary>
internal sealed class RoutingSlipSubscription :
    Subscription
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipSubscription()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    /// <param name="events">The events.</param>
    /// <param name="include">The include.</param>
    /// <param name="activityName">The activity name.</param>
    /// <param name="message">The message to process.</param>
    public RoutingSlipSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents include, string? activityName = null,
        MessageEnvelope? message = null)
    {
        Include = include;
        ActivityName = activityName;
        Address = address;
        Events = events;
        Message = message;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="subscription">The subscription.</param>
    public RoutingSlipSubscription(Subscription subscription)
    {
        if (subscription.Address == null)
            throw new SerializationException("A subscription address is required");

        Address = subscription.Address;
        Events = subscription.Events;
        Include = subscription.Include;
        Message = subscription.Message;
        ActivityName = subscription.ActivityName;
    }

    /// <summary>Gets or sets the address.</summary>
    public Uri Address { get; set; } = null!;
    /// <summary>Gets or sets the events.</summary>
    public RoutingSlipEvents Events { get; set; }
    /// <summary>Gets or sets the include.</summary>
    public RoutingSlipEventContents Include { get; set; }
    /// <summary>Gets or sets the message.</summary>
    public MessageEnvelope? Message { get; set; }
    /// <summary>Gets or sets the activity name.</summary>
    public string? ActivityName { get; set; }
}
