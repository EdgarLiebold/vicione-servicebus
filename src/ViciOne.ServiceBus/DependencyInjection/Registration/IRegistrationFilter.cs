using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Processes registration pipeline stages.</summary>
public interface IRegistrationFilter
{
    /// <summary>Determines whether the supplied value matches.</summary>
    /// <param name="registration">The registration.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(IRegistration registration);
}
