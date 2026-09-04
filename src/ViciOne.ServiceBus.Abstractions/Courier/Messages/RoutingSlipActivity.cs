using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

[Serializable]
public class RoutingSlipActivity :
    Activity
{
    public RoutingSlipActivity()
    {
    }

    public RoutingSlipActivity(string name, Uri address, IDictionary<string, object> arguments)
    {
        Name = name;
        Address = address;
        Arguments = arguments;
    }

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

    public string Name { get; set; } = null!;
    public Uri Address { get; set; } = null!;
    public IDictionary<string, object> Arguments { get; set; } = null!;
}
