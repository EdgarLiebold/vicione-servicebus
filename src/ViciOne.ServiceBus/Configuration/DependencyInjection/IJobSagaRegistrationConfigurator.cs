using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job saga registration.</summary>
public interface IJobSagaRegistrationConfigurator
{
    /// <summary>Configure all three saga endpoints (using the same configuration).</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The job saga registration configurator produced by the operation.</returns>
    IJobSagaRegistrationConfigurator Endpoints(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Configure the JobAttemptSaga endpoint.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The job saga registration configurator produced by the operation.</returns>
    IJobSagaRegistrationConfigurator JobAttemptEndpoint(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Configure the JobSaga endpoint.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The job saga registration configurator produced by the operation.</returns>
    IJobSagaRegistrationConfigurator JobEndpoint(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Configure the JobTypeSaga endpoint.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The job saga registration configurator produced by the operation.</returns>
    IJobSagaRegistrationConfigurator JobTypeEndpoint(Action<IEndpointRegistrationConfigurator> configure);

    /// <summary>Internally used by the saga repositories to register as the saga repository for the job sagas.</summary>
    /// <param name="registrationProvider">The registration provider.</param>
    /// <returns>The configured repository registration provider.</returns>
    IJobSagaRegistrationConfigurator UseRepositoryRegistrationProvider(ISagaRepositoryRegistrationProvider registrationProvider);
}
