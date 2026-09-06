using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Configures registrations that own coordinated primary and companion endpoints.</summary>
public interface IConsumerKindCompanionConfigurator
{
    /// <summary>Attempts to configure both endpoints for a registration.</summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="primaryEndpointConfigurator">The primary endpoint to configure.</param>
    /// <param name="companionEndpointConfigurator">The companion endpoint to configure.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <returns><see langword="true"/> when the type is owned and both endpoints were configured; otherwise, <see langword="false"/>.</returns>
    bool TryConfigurePair(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        IReceiveEndpointConfigurator companionEndpointConfigurator, IRegistrationContext registrationContext);

    /// <summary>Attempts to configure the primary endpoint for a registration whose companion endpoint already has an address.</summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="primaryEndpointConfigurator">The primary endpoint to configure.</param>
    /// <param name="companionAddress">The address of the companion endpoint.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <returns><see langword="true"/> when the type is owned and its primary endpoint was configured; otherwise, <see langword="false"/>.</returns>
    bool TryConfigurePrimary(Type registrationType, IReceiveEndpointConfigurator primaryEndpointConfigurator,
        Uri companionAddress, IRegistrationContext registrationContext);

    /// <summary>Attempts to configure the companion endpoint for a registration.</summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="companionEndpointConfigurator">The companion endpoint to configure.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <returns><see langword="true"/> when the type is owned and its companion endpoint was configured; otherwise, <see langword="false"/>.</returns>
    bool TryConfigureCompanion(Type registrationType, IReceiveEndpointConfigurator companionEndpointConfigurator,
        IRegistrationContext registrationContext);
}
