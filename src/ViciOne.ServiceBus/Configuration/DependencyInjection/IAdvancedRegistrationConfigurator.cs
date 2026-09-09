using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Exposes infrastructure-level operations shared by registration configurators.</summary>
public interface IAdvancedRegistrationConfigurator
{
    /// <summary>Gets the registrar.</summary>
    IContainerRegistrar Registrar { get; }

    /// <summary>Adds a typed endpoint definition for an existing registration.</summary>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="registration">The registration associated with the endpoint.</param>
    /// <param name="settings">The settings that control the operation.</param>
    void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class;

    /// <summary>Gets or add registration completion participant.</summary>
    /// <typeparam name="TParticipant">The participant type.</typeparam>
    /// <param name="factory">Creates the participant when it has not been registered.</param>
    /// <returns>The existing or newly registered participant.</returns>
    TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant;
}
