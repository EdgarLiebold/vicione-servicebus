using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Defines the operations required by activity.</summary>
public interface Activity
{
    /// <summary>Gets the name.</summary>
    string Name { get; }

    /// <summary>Gets the address.</summary>
    Uri Address { get; }

    /// <summary>Gets the arguments.</summary>
    IDictionary<string, object> Arguments { get; }
}
