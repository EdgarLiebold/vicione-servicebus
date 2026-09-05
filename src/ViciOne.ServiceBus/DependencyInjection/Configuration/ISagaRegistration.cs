using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga registration.
/// </summary>
public interface ISagaRegistration :
    IRegistration
{
    /// <summary>
    /// Gets the state-machine implementation type, or <see langword="null" /> for a class saga.
    /// </summary>
    Type? StateMachineType { get; }

    /// <summary>
    /// Adds configure action to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? configure)
        where T : class;

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>
    /// Gets definition.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    ISagaDefinition GetDefinition(IRegistrationContext context);
}
