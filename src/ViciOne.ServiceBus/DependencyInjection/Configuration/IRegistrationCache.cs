using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for registration cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IRegistrationCache<out T>
{
    /// <summary>
    /// Gets the values value.
    /// </summary>
    IEnumerable<T> Values { get; }
}
