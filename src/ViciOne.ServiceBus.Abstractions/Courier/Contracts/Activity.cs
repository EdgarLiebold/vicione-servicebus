using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>
/// Defines the contract for activity.
/// </summary>
public interface Activity
{
    /// <summary>
    /// Gets the name value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    Uri Address { get; }

    /// <summary>
    /// Gets the arguments value.
    /// </summary>
    IDictionary<string, object> Arguments { get; }
}
