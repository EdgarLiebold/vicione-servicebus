using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes state for rider registration operations.</summary>
public interface IRiderRegistrationContext :
    IRegistrationContext
{
    /// <summary>Returns registrations owned by the rider.</summary>
    /// <typeparam name="T">The registration category.</typeparam>
    /// <returns>The rider-specific registrations.</returns>
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;
}
