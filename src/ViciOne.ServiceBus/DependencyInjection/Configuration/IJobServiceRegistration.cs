using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for job service registration.
/// </summary>
public interface IJobServiceRegistration :
    IRegistration
{
    /// <summary>
    /// Gets the endpoint registration configurator value.
    /// </summary>
    IEndpointRegistrationConfigurator EndpointRegistrationConfigurator { get; }

    /// <summary>
    /// Gets the endpoint definition value.
    /// </summary>
    IEndpointDefinition EndpointDefinition { get; }

    /// <summary>
    /// Adds configure action to the configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void AddConfigureAction(Action<JobConsumerOptions>? configure);

    /// <summary>
    /// Adds receive endpoint dependency to the configuration.
    /// </summary>
    /// <param name="dependency">The dependency value.</param>
    void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency);

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="instanceConfigurator">The instance configurator value.</param>
    /// <param name="context">The operation context.</param>
    void Configure(IServiceInstanceConfigurator instanceConfigurator, IRegistrationContext context);
}
