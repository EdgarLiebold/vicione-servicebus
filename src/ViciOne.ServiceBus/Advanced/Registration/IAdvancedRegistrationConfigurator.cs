using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Exposes infrastructure-level operations shared by registration configurators.</summary>
public interface IAdvancedRegistrationConfigurator
{
    /// <summary>Gets the container-specific registrar that owns component registrations.</summary>
    IContainerRegistrar Registrar { get; }

    /// <summary>Adds a typed endpoint definition for an existing registration.</summary>
    /// <typeparam name="TDefinition">The endpoint definition implementation.</typeparam>
    /// <typeparam name="T">The registered component associated with the endpoint.</typeparam>
    /// <param name="registration">The registration associated with the endpoint.</param>
    /// <param name="settings">Optional endpoint settings passed to the definition constructor.</param>
    void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class;

    /// <summary>Gets an existing capability completion participant or registers one.</summary>
    /// <typeparam name="TParticipant">The capability-specific completion participant.</typeparam>
    /// <param name="factory">Creates the participant when it has not been registered.</param>
    /// <returns>The existing or newly registered participant.</returns>
    TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant;
}
