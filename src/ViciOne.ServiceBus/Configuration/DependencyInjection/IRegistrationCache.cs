using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to registration data.</summary>
/// <typeparam name="T">The cached registration representation.</typeparam>
public interface IRegistrationCache<out T>
{
    /// <summary>Gets the cached registrations.</summary>
    IEnumerable<T> Values { get; }
}
