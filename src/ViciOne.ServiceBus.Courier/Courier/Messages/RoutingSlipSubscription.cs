using System;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes a validated routing-slip event subscription.</summary>
internal sealed class RoutingSlipSubscription :
    ISubscription
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipSubscription()
    {
    }

    /// <summary>Creates a subscription from validated event and payload selections.</summary>
    /// <param name="address">The destination that receives matching events.</param>
    /// <param name="events">The lifecycle events that trigger delivery.</param>
    /// <param name="include">The optional routing-slip data included in delivered events.</param>
    /// <param name="activityName">The activity-name filter, when delivery is limited to one activity.</param>
    /// <param name="message">The custom subscription message, when configured.</param>
    public RoutingSlipSubscription(Uri address, RoutingSlipEvents events, RoutingSlipEventContents include, string? activityName = null,
        MessageEnvelope? message = null)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (activityName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(activityName);

        Include = RoutingSlipSubscriptionSelection.Validate(include, nameof(include));
        ActivityName = activityName;
        Address = address;
        Events = RoutingSlipSubscriptionSelection.Validate(events, nameof(events));
        Message = message;
    }

    /// <summary>Creates a validated snapshot of received subscription data.</summary>
    /// <param name="subscription">The received subscription to copy.</param>
    public RoutingSlipSubscription(ISubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        if (subscription.Address == null)
            throw new SerializationException("A routing-slip subscription address is required.");

        Address = subscription.Address;
        Events = ValidateReceived(subscription.Events);
        Include = ValidateReceived(subscription.Include);
        Message = subscription.Message;
        ActivityName = ValidateReceived(subscription.ActivityName);
    }

    /// <summary>Gets or sets the destination that receives matching events.</summary>
    public Uri Address { get; set; } = null!;
    /// <summary>Gets or sets the lifecycle events that trigger delivery.</summary>
    public RoutingSlipEvents Events { get; set; }
    /// <summary>Gets or sets the optional routing-slip data included in delivered events.</summary>
    public RoutingSlipEventContents Include { get; set; }
    /// <summary>Gets or sets the custom subscription message, when configured.</summary>
    public MessageEnvelope? Message { get; set; }
    /// <summary>Gets or sets the activity-name filter, when delivery is limited to one activity.</summary>
    public string? ActivityName { get; set; }

    static RoutingSlipEvents ValidateReceived(RoutingSlipEvents events)
    {
        try
        {
            return RoutingSlipSubscriptionSelection.Validate(events, nameof(ISubscription.Events));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new SerializationException("The routing-slip subscription contains an invalid event selection.", exception);
        }
    }

    static RoutingSlipEventContents ValidateReceived(RoutingSlipEventContents contents)
    {
        try
        {
            return RoutingSlipSubscriptionSelection.Validate(contents, nameof(ISubscription.Include));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new SerializationException("The routing-slip subscription contains an invalid content selection.", exception);
        }
    }

    static string? ValidateReceived(string? activityName)
    {
        if (activityName is null)
            return null;

        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
            return activityName;
        }
        catch (ArgumentException exception)
        {
            throw new SerializationException("The routing-slip subscription contains an invalid activity-name filter.", exception);
        }
    }
}
