using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Executes the routing slip activity.</summary>
public class RoutingSlipActivity :
    Activity
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipActivity()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="name">The name.</param>
    /// <param name="address">The address.</param>
    /// <param name="arguments">The arguments.</param>
    public RoutingSlipActivity(string name, Uri address, IDictionary<string, object> arguments)
    {
        Name = name;
        Address = address;
        Arguments = arguments;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
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

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the address.</summary>
    public Uri Address { get; set; } = null!;
    /// <summary>Gets or sets the arguments.</summary>
    public IDictionary<string, object> Arguments { get; set; } = null!;
}
