using System;
using ViciOne.ServiceBus.Advanced.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by job service registration.</summary>
public interface IJobServiceRegistration :
    IConsumerKindHost
{
    /// <summary>Gets the endpoint registration configurator.</summary>
    IEndpointRegistrationConfigurator EndpointRegistrationConfigurator { get; }

    /// <summary>Adds configure action to the configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void AddConfigureAction(Action<JobConsumerOptions>? configure);

    /// <summary>Adds receive endpoint dependency to the configuration.</summary>
    /// <param name="dependency">The dependency.</param>
    void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency);

}
