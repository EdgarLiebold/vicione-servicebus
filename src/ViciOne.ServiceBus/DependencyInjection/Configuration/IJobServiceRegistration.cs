using System;
using ViciOne.ServiceBus.Advanced.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for job service registration.
/// </summary>
public interface IJobServiceRegistration :
    IConsumerKindHost
{
    /// <summary>
    /// Gets the endpoint registration configurator value.
    /// </summary>
    IEndpointRegistrationConfigurator EndpointRegistrationConfigurator { get; }

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

}
