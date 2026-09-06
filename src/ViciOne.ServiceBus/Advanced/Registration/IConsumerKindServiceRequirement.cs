using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Identifies registration types that require a service-instance host.</summary>
public interface IConsumerKindServiceRequirement
{
    /// <summary>Determines whether the registration type must be hosted by a service instance.</summary>
    /// <param name="registrationType">The registration type.</param>
    /// <returns><see langword="true"/> when a service-instance host is required; otherwise, <see langword="false"/>.</returns>
    bool RequiresServiceInstance(Type registrationType);
}
