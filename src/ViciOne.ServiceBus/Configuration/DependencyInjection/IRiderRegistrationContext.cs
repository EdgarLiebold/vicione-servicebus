using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for rider registration context.
/// </summary>
public interface IRiderRegistrationContext :
    IRegistrationContext
{
    /// <summary>
    /// Gets registrations.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    IEnumerable<T> GetRegistrations<T>()
        where T : class, IRegistration;
}
