using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip subscription implementation.
/// </summary>
[Serializable]
public class RoutingSlipSubscription :
    Subscription
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipSubscription()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="events">The events value.</param>
    /// <param name="include">The include value.</param>
    /// <param name="activityName">The activity name value.</param>
    /// <param name="message">The message value.</param>
    public RoutingSlipSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents include, string? activityName = null,
        MessageEnvelope? message = null)
    {
        Include = include;
        ActivityName = activityName;
        Address = address;
        Events = events;
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="subscription">The subscription value.</param>
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

    /// <summary>
    /// Gets or sets the address value.
    /// </summary>
    public Uri Address { get; set; } = null!;
    /// <summary>
    /// Gets or sets the events value.
    /// </summary>
    public RoutingSlipEvents Events { get; set; }
    /// <summary>
    /// Gets or sets the include value.
    /// </summary>
    public RoutingSlipEventContents Include { get; set; }
    /// <summary>
    /// Gets or sets the message value.
    /// </summary>
    public MessageEnvelope? Message { get; set; }
    /// <summary>
    /// Gets or sets the activity name value.
    /// </summary>
    public string? ActivityName { get; set; }
}
