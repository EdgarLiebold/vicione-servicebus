using System;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Exposes infrastructure-level operations shared by registration configurators.</summary>
public interface IAdvancedRegistrationConfigurator
{
    /// <summary>Gets the container registrar used by capability and provider integrations.</summary>
    IContainerRegistrar Registrar { get; }

    /// <summary>Adds a typed endpoint definition for an existing registration.</summary>
    /// <typeparam name="TDefinition">The endpoint definition type.</typeparam>
    /// <typeparam name="T">The registered service type.</typeparam>
    /// <param name="registration">The registration associated with the endpoint.</param>
    /// <param name="settings">The optional endpoint settings.</param>
    void AddEndpoint<TDefinition, T>(IRegistration registration, IEndpointSettings<IEndpointDefinition<T>>? settings = null)
        where TDefinition : class, IEndpointDefinition<T>
        where T : class;

    /// <summary>Sets the default request timeout from individual duration components.</summary>
    /// <param name="d">The number of days.</param>
    /// <param name="h">The number of hours.</param>
    /// <param name="m">The number of minutes.</param>
    /// <param name="s">The number of seconds.</param>
    /// <param name="ms">The number of milliseconds.</param>
    void SetDefaultRequestTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null);

    /// <summary>Gets the capability registration-completion participant, creating it when necessary.</summary>
    /// <typeparam name="TParticipant">The participant type.</typeparam>
    /// <param name="factory">Creates the participant when it has not been registered.</param>
    /// <returns>The existing or newly registered participant.</returns>
    TParticipant GetOrAddRegistrationCompletionParticipant<TParticipant>(Func<TParticipant> factory)
        where TParticipant : class, IRegistrationCompletionParticipant;
}
