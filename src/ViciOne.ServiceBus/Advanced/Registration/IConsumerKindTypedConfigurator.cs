using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Configures a statically typed consumer-kind registration.</summary>
public interface IConsumerKindTypedConfigurator
{
    /// <summary>Attempts to configure a typed registration and an optional capability-specific callback on an existing endpoint.</summary>
    /// <typeparam name="TRegistration">The registration type.</typeparam>
    /// <param name="endpointConfigurator">The endpoint to configure.</param>
    /// <param name="registrationContext">The active registration context.</param>
    /// <param name="configure">An optional capability-specific configuration callback.</param>
    /// <returns><see langword="true"/> when the type is owned and configured by this consumer kind; otherwise, <see langword="false"/>.</returns>
    bool TryConfigure<TRegistration>(IReceiveEndpointConfigurator endpointConfigurator,
        IRegistrationContext registrationContext, Delegate? configure = null)
        where TRegistration : class;
}
