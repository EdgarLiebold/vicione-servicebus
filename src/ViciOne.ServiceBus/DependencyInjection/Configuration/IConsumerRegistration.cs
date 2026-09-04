using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer registration.
/// </summary>
public interface IConsumerRegistration :
    IRegistration
{
    /// <summary>
    /// Adds configure action to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    void AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure)
        where T : class, IConsumer;

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
    IConsumerDefinition GetDefinition(IRegistrationContext context);

    /// <summary>
    /// Gets consumer registration configurator.
    /// </summary>
    /// <param name="registrationConfigurator">The registration configurator value.</param>
    /// <returns>The result of the operation.</returns>
    IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registrationConfigurator);
}
