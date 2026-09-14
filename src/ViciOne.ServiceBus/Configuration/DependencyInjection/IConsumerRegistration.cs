using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents a consumer registration that can be configured on a receive endpoint.</summary>
public interface IConsumerRegistration :
    IRegistration
{
    /// <summary>Gets or sets a value indicating whether the consumer endpoint must be hosted by a service instance.</summary>
    bool RequiresServiceInstance { get; set; }

    /// <summary>Adds an optional callback applied when the consumer is attached to an endpoint.</summary>
    /// <typeparam name="T">The registered consumer implementation.</typeparam>
    /// <param name="configure">The callback that receives the active registration context and consumer configurator.</param>
    void AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure)
        where T : class, IConsumer;

    /// <summary>Attaches the consumer and its middleware to a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    /// <param name="context">The registration context used to resolve definitions and dependencies.</param>
    void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context);

    /// <summary>Resolves the effective consumer definition.</summary>
    /// <param name="context">The registration context that contains an optional explicit definition.</param>
    /// <returns>The explicit definition or the conventional default definition.</returns>
    IConsumerDefinition GetDefinition(IRegistrationContext context);

    /// <summary>Creates the fluent configurator associated with this registration.</summary>
    /// <param name="registrationConfigurator">The owning bus registration configurator.</param>
    /// <returns>A fluent consumer registration configurator.</returns>
    IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registrationConfigurator);
}
