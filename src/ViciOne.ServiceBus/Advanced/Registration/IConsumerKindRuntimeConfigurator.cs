using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Configures a consumer-kind registration selected by its runtime type.</summary>
public interface IConsumerKindRuntimeConfigurator
{
    /// <summary>Attempts to configure a registration owned by this consumer kind on an existing endpoint.</summary>
    /// <param name="registrationType">The registration type.</param>
    /// <param name="endpointConfigurator">The endpoint to configure.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <returns><see langword="true"/> when the type is owned and configured by this consumer kind; otherwise, <see langword="false"/>.</returns>
    bool TryConfigure(Type registrationType, IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext);
}
