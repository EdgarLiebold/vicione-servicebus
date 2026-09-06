using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip activity implementation.
/// </summary>
public class RoutingSlipActivity :
    Activity
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipActivity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="arguments">The arguments value.</param>
    public RoutingSlipActivity(string name, Uri address, IDictionary<string, object> arguments)
    {
        Name = name;
        Address = address;
        Arguments = arguments;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition")]
    public RoutingSlipActivity(Activity activity)
    {
        if (string.IsNullOrEmpty(activity.Name))
            throw new SerializationException("An Activity Name is required");
        if (activity.Address == null)
            throw new SerializationException("An Activity ExecuteAddress is required");

        Name = activity.Name;
        Address = activity.Address;
        Arguments = activity.Arguments ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets the address value.
    /// </summary>
    public Uri Address { get; set; } = null!;
    /// <summary>
    /// Gets or sets the arguments value.
    /// </summary>
    public IDictionary<string, object> Arguments { get; set; } = null!;
}
