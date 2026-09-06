using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by consumer registration.</summary>
public interface IConsumerRegistration :
    IRegistration
{
    /// <summary>Gets or sets a value indicating whether the consumer endpoint must be hosted by a service instance.</summary>
    bool RequiresServiceInstance { get; set; }

    /// <summary>Adds configure action to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure)
        where T : class, IConsumer;

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Gets definition.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The definition.</returns>
    IConsumerDefinition GetDefinition(IRegistrationContext context);

    /// <summary>Gets consumer registration configurator.</summary>
    /// <param name="registrationConfigurator">The registration configurator.</param>
    /// <returns>The consumer registration configurator.</returns>
    IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registrationConfigurator);
}
