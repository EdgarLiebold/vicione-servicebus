using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a job saga registration configurator implementation.
/// </summary>
public class JobSagaRegistrationConfigurator :
    IJobSagaRegistrationConfigurator
{
    readonly IBusRegistrationConfigurator _configurator;
    ISagaRegistrationConfigurator<JobAttemptSaga> _jobAttemptConfigurator;
    ISagaRegistrationConfigurator<JobSaga> _jobConfigurator;
    ISagaRegistrationConfigurator<JobTypeSaga> _jobTypeConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public JobSagaRegistrationConfigurator(IBusRegistrationConfigurator configurator, Action<JobSagaOptions>? configure)
    {
        _configurator = configurator;

        configurator.Services.AddOptions<JobSagaOptions>()
            .Configure(options => configure?.Invoke(options));

        _jobTypeConfigurator = configurator.AddSagaStateMachine<JobTypeStateMachine, JobTypeSaga, JobTypeSagaDefinition>();
        _jobConfigurator = configurator.AddSagaStateMachine<JobStateMachine, JobSaga, JobSagaDefinition>();
        _jobAttemptConfigurator = configurator.AddSagaStateMachine<JobAttemptStateMachine, JobAttemptSaga, JobAttemptSagaDefinition>();
    }

    /// <summary>
    /// Performs the endpoints operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IJobSagaRegistrationConfigurator Endpoints(Action<IEndpointRegistrationConfigurator> configure)
    {
        _jobAttemptConfigurator = _jobAttemptConfigurator.Endpoint(configure);
        _jobConfigurator = _jobConfigurator.Endpoint(configure);
        _jobTypeConfigurator = _jobTypeConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>
    /// Performs the job attempt endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IJobSagaRegistrationConfigurator JobAttemptEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        _jobAttemptConfigurator = _jobAttemptConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>
    /// Performs the job endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IJobSagaRegistrationConfigurator JobEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        _jobConfigurator = _jobConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>
    /// Performs the job type endpoint operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IJobSagaRegistrationConfigurator JobTypeEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        _jobTypeConfigurator = _jobTypeConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>
    /// Configures repository registration provider for the current pipeline.
    /// </summary>
    /// <param name="registrationProvider">The registration provider value.</param>
    /// <returns>The result of the operation.</returns>
    public IJobSagaRegistrationConfigurator UseRepositoryRegistrationProvider(ISagaRepositoryRegistrationProvider registrationProvider)
    {
        registrationProvider.Configure(_jobAttemptConfigurator);
        registrationProvider.Configure(_jobConfigurator);
        registrationProvider.Configure(_jobTypeConfigurator);

        return this;
    }
}
