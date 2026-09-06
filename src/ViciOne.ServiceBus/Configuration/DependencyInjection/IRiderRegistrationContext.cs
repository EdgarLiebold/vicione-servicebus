using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes state for rider registration operations.</summary>
public interface IRiderRegistrationContext :
    IRegistrationContext
{
    /// <summary>Gets registrations.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The registrations.</returns>
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;
}
