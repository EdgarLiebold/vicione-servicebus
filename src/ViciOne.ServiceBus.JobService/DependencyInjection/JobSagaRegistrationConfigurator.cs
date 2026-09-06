using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Registers all three job coordination state machines with one repository provider.</summary>
internal sealed class JobSagaRegistrationConfigurator :
    IJobSagaRegistrationConfigurator
{
    ISagaRegistrationConfigurator<JobAttemptSaga> _jobAttemptConfigurator;
    ISagaRegistrationConfigurator<JobSaga> _jobConfigurator;
    ISagaRegistrationConfigurator<JobTypeSaga> _jobTypeConfigurator;

    /// <summary>Registers the three state machines and validates their shared runtime options.</summary>
    /// <param name="configurator">The bus registration receiving the state machines.</param>
    /// <param name="configure">The optional callback that customizes shared saga options.</param>
    public JobSagaRegistrationConfigurator(IBusRegistrationConfigurator configurator, Action<JobSagaOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        JobServiceCorrelationConventions.Register();
        JobConsumerConventionRegistration.Register();

        var optionsBuilder = configurator.Services.AddOptions<JobSagaOptions>();
        if (configure is not null)
            optionsBuilder.Configure(configure);

        optionsBuilder
            .Validate(
                static options => options.SlotWaitTime >= TimeSpan.FromSeconds(1),
                "Job saga for bus 'default': SlotWaitTime must be at least one second. Set SlotWaitTime to one second or longer.")
            .Validate(
                static options => options.StatusCheckInterval >= TimeSpan.FromSeconds(30),
                "Job saga for bus 'default': StatusCheckInterval must be at least 30 seconds. Set StatusCheckInterval to 30 seconds or longer.")
            .Validate(
                static options => options.HeartbeatTimeout > TimeSpan.Zero,
                "Job saga for bus 'default': HeartbeatTimeout must be greater than zero. Set HeartbeatTimeout to a positive duration.")
            .Validate(
                static options => options.ConcurrentMessageLimit is null or > 0,
                "Job saga for bus 'default': ConcurrentMessageLimit must be greater than zero when specified. Set it to a positive value or leave it unset.")
            .Validate(
                static options => options.SuspectJobRetryCount >= 0,
                "Job saga for bus 'default': SuspectJobRetryCount must not be negative. Set it to zero or a positive value.")
            .Validate(
                static options => options.SuspectJobRetryDelay is null || options.SuspectJobRetryDelay > TimeSpan.Zero,
                "Job saga for bus 'default': SuspectJobRetryDelay must be greater than zero when specified. Set it to a positive duration or leave it unset.")
            .ValidateOnStart();

        _jobTypeConfigurator = configurator.AddSagaStateMachine<JobTypeStateMachine, JobTypeSaga, JobTypeSagaDefinition>();
        _jobConfigurator = configurator.AddSagaStateMachine<JobStateMachine, JobSaga, JobSagaDefinition>();
        _jobAttemptConfigurator = configurator.AddSagaStateMachine<JobAttemptStateMachine, JobAttemptSaga, JobAttemptSagaDefinition>();
    }

    /// <summary>Applies the same endpoint configuration to every job state machine.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    public IJobSagaRegistrationConfigurator ConfigureEndpoints(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _jobAttemptConfigurator = _jobAttemptConfigurator.Endpoint(configure);
        _jobConfigurator = _jobConfigurator.Endpoint(configure);
        _jobTypeConfigurator = _jobTypeConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>Configures the endpoint that persists individual execution attempts.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    public IJobSagaRegistrationConfigurator ConfigureJobAttemptEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _jobAttemptConfigurator = _jobAttemptConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>Configures the endpoint that persists job lifecycle state.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    public IJobSagaRegistrationConfigurator ConfigureJobEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _jobConfigurator = _jobConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>Configures the endpoint that coordinates capacity for each job type.</summary>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>This configurator.</returns>
    public IJobSagaRegistrationConfigurator ConfigureJobTypeEndpoint(Action<IEndpointRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        _jobTypeConfigurator = _jobTypeConfigurator.Endpoint(configure);
        return this;
    }

    /// <summary>Configures one repository provider for all job state machines.</summary>
    /// <param name="registrationProvider">The provider that configures saga persistence.</param>
    /// <returns>This configurator.</returns>
    public IJobSagaRegistrationConfigurator UseRepositoryRegistrationProvider(ISagaRepositoryRegistrationProvider registrationProvider)
    {
        ArgumentNullException.ThrowIfNull(registrationProvider);

        registrationProvider.Configure(_jobAttemptConfigurator);
        registrationProvider.Configure(_jobConfigurator);
        registrationProvider.Configure(_jobTypeConfigurator);

        return this;
    }
}
