using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job coordination endpoints and their shared persistence provider.</summary>
public interface IJobSagaRegistrationConfigurator
{
    /// <summary>Applies the same endpoint configuration to all three job coordination state machines.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    IJobSagaRegistrationConfigurator ConfigureEndpoints(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Configures the endpoint that persists and supervises individual execution attempts.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    IJobSagaRegistrationConfigurator ConfigureJobAttemptEndpoint(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Configures the endpoint that persists complete job lifecycles.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    IJobSagaRegistrationConfigurator ConfigureJobEndpoint(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Configures the endpoint that coordinates capacity for each job type.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    IJobSagaRegistrationConfigurator ConfigureJobTypeEndpoint(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Uses one persistence provider for all three job coordination state machines.</summary>
    /// <param name="registrationProvider">The provider that registers saga persistence.</param>
    /// <returns>This configurator.</returns>
    IJobSagaRegistrationConfigurator UseRepositoryRegistrationProvider(ISagaRepositoryRegistrationProvider registrationProvider);
}
