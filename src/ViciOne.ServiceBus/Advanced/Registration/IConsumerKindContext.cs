using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides the bus-scoped registration view used by a consumer kind.</summary>
public interface IConsumerKindContext
{
    /// <summary>Gets the services and registrations available to the active bus.</summary>
    IRegistrationContext RegistrationContext { get; }

    /// <summary>Gets the formatter used to derive endpoint names.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>Enumerates enabled, selected registrations that have not already been configured.</summary>
    /// <typeparam name="TRegistration">The registration type.</typeparam>
    /// <returns>The selected registrations of the requested type.</returns>
    IEnumerable<TRegistration> GetRegistrations<TRegistration>()
        where TRegistration : class, IRegistration;
}
