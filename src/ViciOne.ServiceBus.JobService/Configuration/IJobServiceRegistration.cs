using System;
using ViciOne.ServiceBus.Advanced.Registration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Tracks the local job runtime and the endpoints that must stop after it.</summary>
internal interface IJobServiceRegistration :
    IConsumerKindHost
{
    /// <summary>Gets the configuration of the local job-execution endpoint.</summary>
    IEndpointRegistrationConfigurator EndpointRegistrationConfigurator { get; }

    /// <summary>Adds a callback that configures the local job runtime.</summary>
    /// <param name="configure">The runtime configuration callback.</param>
    void AddConfigureAction(Action<JobConsumerOptions> configure);

    /// <summary>Adds an endpoint that must be ready before discovered job-consumer endpoints start.</summary>
    /// <param name="dependency">The required receive endpoint.</param>
    void AddReceiveEndpointDependency(IReceiveEndpointConfigurator dependency);
}
