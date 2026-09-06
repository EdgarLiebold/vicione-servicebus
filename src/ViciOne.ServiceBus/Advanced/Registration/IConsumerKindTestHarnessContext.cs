using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Provides the pre-container registration view used to attach test-harness observations to consumer kinds.</summary>
public interface IConsumerKindTestHarnessContext
{
    /// <summary>Gets the consumer-kind category name.</summary>
    string KindName { get; }

    /// <summary>Enumerates registrations owned by the active bus registration.</summary>
    /// <typeparam name="TRegistration">The registration type.</typeparam>
    /// <returns>The registrations visible before the service provider is built.</returns>
    IEnumerable<TRegistration> GetRegistrations<TRegistration>()
        where TRegistration : class, IRegistration;

    /// <summary>Attaches test-harness observation for a registration and its supporting types.</summary>
    /// <param name="registrationType">The registration type to observe.</param>
    /// <param name="supportingTypes">Capability-specific implementation types required by the observation.</param>
    void Observe(Type registrationType, params Type[] supportingTypes);
}
