using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides the bus-scoped registration view used by a consumer kind.</summary>
public interface IConsumerKindContext
{
    /// <summary>Gets the active bus registration context.</summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the endpoint-name formatter selected for the bus.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>Enumerates enabled, selected registrations that have not already been configured.</summary>
    /// <typeparam name="TRegistration">The registration contract.</typeparam>
    /// <returns>The selected registrations.</returns>
    IEnumerable<TRegistration> GetRegistrations<TRegistration>()
        where TRegistration : class, IRegistration;
}
