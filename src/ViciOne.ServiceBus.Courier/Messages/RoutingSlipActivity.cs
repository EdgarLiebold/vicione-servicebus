using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes an activity entry in a routing-slip itinerary.</summary>
internal sealed class RoutingSlipActivity :
    Activity
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipActivity()
    {
    }

    /// <summary>Creates an activity with an isolated argument snapshot.</summary>
    /// <param name="name">The non-empty activity name.</param>
    /// <param name="address">The activity execution address.</param>
    /// <param name="arguments">The activity arguments.</param>
    public RoutingSlipActivity(string name, Uri address, IDictionary<string, object> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(arguments);

        Name = name;
        Address = address;
        Arguments = Snapshot(arguments);
    }

    /// <summary>Creates a validated snapshot of received activity data.</summary>
    /// <param name="activity">The received activity to copy.</param>
    public RoutingSlipActivity(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (string.IsNullOrWhiteSpace(activity.Name))
            throw new SerializationException("A routing-slip activity name is required.");
        if (activity.Address == null)
            throw new SerializationException("A routing-slip activity execution address is required.");

        Name = activity.Name;
        Address = activity.Address;
        Arguments = Snapshot(activity.Arguments ?? new Dictionary<string, object>());
    }

    /// <summary>Gets or sets the activity name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Gets or sets the activity execution address.</summary>
    public Uri Address { get; set; } = null!;
    /// <summary>Gets or sets the activity arguments.</summary>
    public IReadOnlyDictionary<string, object> Arguments { get; set; } = null!;

    static IReadOnlyDictionary<string, object> Snapshot(IEnumerable<KeyValuePair<string, object>> arguments) =>
        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(arguments, StringComparer.OrdinalIgnoreCase));
}
