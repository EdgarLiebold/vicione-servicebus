using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Partitioning;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job-service coordination endpoints for a service instance.</summary>
/// <typeparam name="TReceiveEndpointConfigurator">The receive endpoint configurator type.</typeparam>
internal sealed class JobServiceConfigurator<TReceiveEndpointConfigurator> :
    IJobServiceConfigurator,
    ISpecification
    where TReceiveEndpointConfigurator : IReceiveEndpointConfigurator
{
    readonly IReceiveConfigurator<TReceiveEndpointConfigurator> _busConfigurator;
    readonly JobServiceOptions _options;
    bool _endpointsConfigured;
    ISagaRepository<JobAttemptSaga>? _jobAttemptRepository;
    IReceiveEndpointConfigurator? _jobAttemptSagaEndpointConfigurator;
    ISagaRepository<JobSaga>? _jobRepository;
    IReceiveEndpointConfigurator? _jobSagaEndpointConfigurator;
    ISagaRepository<JobTypeSaga>? _jobTypeRepository;
    IReceiveEndpointConfigurator? _jobTypeSagaEndpointConfigurator;

    /// <summary>Binds job coordination, supervision, and repository configuration to one service instance.</summary>
    /// <param name="instanceConfigurator">The service instance that owns the job endpoints.</param>
    /// <param name="options">Explicit endpoint and supervision options, or <see langword="null" /> to use the instance-owned options.</param>
    public JobServiceConfigurator(IServiceInstanceConfigurator<TReceiveEndpointConfigurator> instanceConfigurator, JobServiceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(instanceConfigurator);

        JobServiceCorrelationConventions.Register();
        JobConsumerConventionRegistration.Register();

        _busConfigurator = instanceConfigurator.BusConfigurator;

        _options = options != null
            ? instanceConfigurator.Options(options)
            : instanceConfigurator.Options<JobServiceOptions>();

        var settings = new InstanceJobServiceSettings(_options);
        settings.ApplyConfiguration(instanceConfigurator.InstanceEndpointConfigurator);

        settings.Runtime.ConfigureSuperviseJobConsumer(instanceConfigurator.InstanceEndpointConfigurator);

        if (instanceConfigurator.BusConfigurator is IBusObserverConnector connector)
            connector.ConnectBusObserver(new JobServiceBusObserver(settings.Runtime));

        instanceConfigurator.AddSpecification(this);

        _options.JobTypeEndpointName = instanceConfigurator.EndpointNameFormatter.Saga<JobTypeSaga>();
        _options.JobEndpointName = instanceConfigurator.EndpointNameFormatter.Saga<JobSaga>();
        _options.JobAttemptEndpointName = instanceConfigurator.EndpointNameFormatter.Saga<JobAttemptSaga>();

        instanceConfigurator.ConnectEndpointConfigurationObserver(new JobServiceEndpointConfigurationObserver(settings, cfg =>
        {
            if (_jobTypeSagaEndpointConfigurator != null)
                cfg.AddDependency(_jobTypeSagaEndpointConfigurator);
            if (_jobSagaEndpointConfigurator != null)
                cfg.AddDependency(_jobSagaEndpointConfigurator);
            if (_jobAttemptSagaEndpointConfigurator != null)
                cfg.AddDependency(_jobAttemptSagaEndpointConfigurator);
        }));
    }

    /// <inheritdoc />
    public ISagaRepository<JobTypeSaga> JobTypeRepository
    {
        set => _jobTypeRepository = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public ISagaRepository<JobSaga> JobRepository
    {
        set => _jobRepository = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public ISagaRepository<JobAttemptSaga> JobAttemptRepository
    {
        set => _jobAttemptRepository = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public string JobTypeEndpointName
    {
        set => _options.JobTypeEndpointName = value;
    }

    /// <inheritdoc />
    public string JobEndpointName
    {
        set => _options.JobEndpointName = value;
    }

    /// <inheritdoc />
    public string JobAttemptEndpointName
    {
        set => _options.JobAttemptEndpointName = value;
    }

    /// <inheritdoc />
    public TimeSpan HeartbeatInterval
    {
        set => _options.HeartbeatInterval = value;
    }

    /// <inheritdoc />
    public TimeSpan HeartbeatTimeout
    {
        set => _options.HeartbeatTimeout = value;
    }

    /// <inheritdoc />
    public TimeSpan RejectedJobDelay
    {
        set => _options.RejectedJobDelay = value;
    }

    /// <inheritdoc />
    public TimeProvider TimeProvider
    {
        set => _options.TimeProvider = value;
    }

    /// <inheritdoc />
    public TimeSpan SlotWaitTime
    {
        set => _options.SlotWaitTime = value;
    }

    /// <inheritdoc />
    public TimeSpan StatusCheckInterval
    {
        set => _options.StatusCheckInterval = value;
    }

    /// <inheritdoc />
    public int SuspectJobRetryCount
    {
        set => _options.SuspectJobRetryCount = value;
    }

    /// <inheritdoc />
    public TimeSpan? SuspectJobRetryDelay
    {
        set => _options.SuspectJobRetryDelay = value;
    }

    /// <inheritdoc />
    public int? ConcurrentMessageLimit
    {
        set => _options.ConcurrentMessageLimit = value;
    }

    /// <inheritdoc />
    public bool FinalizeCompleted
    {
        set => _options.FinalizeCompleted = value;
    }

    /// <inheritdoc />
    public Func<string, TimeZoneInfo?>? TimeZoneResolver
    {
        set => _options.TimeZoneResolver = value;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        ISpecification options = _options;

        return options.Validate();
    }

    /// <summary>Creates the three coordination endpoints once and optionally enables scoped middleware.</summary>
    /// <param name="context">The registration context used for message scopes, or <see langword="null" /> for containerless configuration.</param>
    public void ConfigureJobServiceEndpoints(IRegistrationContext? context = null)
    {
        if (_endpointsConfigured)
            return;

        void UseVolatileOutbox(IReceiveEndpointConfigurator configurator)
        {
            if (context == null)
                configurator.UseVolatileOutbox();
            else
            {
                configurator.UseMessageScope(context);
                configurator.UseVolatileOutbox(context);
            }
        }

        _busConfigurator.ReceiveEndpoint(_options.JobEndpointName, e =>
        {
            e.UseTechnicalMessageRetry();

            UseVolatileOutbox(e);

            if (_options.ConcurrentMessageLimit.HasValue)
            {
                e.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

                var partition = new PartitionCoordinator(_options.ConcurrentMessageLimit.Value);

                e.UsePartitioner<IJobSubmitted>(partition, p => p.Message.JobId);

                e.UsePartitioner<IJobSlotAllocated>(partition, p => p.Message.JobId);
                e.UsePartitioner<IJobSlotUnavailable>(partition, p => p.Message.JobId);
                e.UsePartitioner<Fault<IAllocateJobSlot>>(partition, p => p.Message.Message.JobId);

                e.UsePartitioner<Fault<IStartJobAttempt>>(partition, p => p.Message.Message.JobId);

                e.UsePartitioner<IJobAttemptCanceled>(partition, p => p.Message.JobId);
                e.UsePartitioner<IJobAttemptCompleted>(partition, p => p.Message.JobId);
                e.UsePartitioner<IJobAttemptFaulted>(partition, p => p.Message.JobId);
                e.UsePartitioner<IJobAttemptStarted>(partition, p => p.Message.JobId);

                e.UsePartitioner<IGetJobState>(partition, p => p.Message.JobId);

                e.UsePartitioner<IJobCompleted>(partition, p => p.Message.JobId);
                e.UsePartitioner<ICancelJob>(partition, p => p.Message.JobId);
                e.UsePartitioner<IRetryJob>(partition, p => p.Message.JobId);
                e.UsePartitioner<IRunJob>(partition, p => p.Message.JobId);

                e.UsePartitioner<ISaveJobCheckpoint>(partition, p => p.Message.JobId);
                e.UsePartitioner<ISetJobProgress>(partition, p => p.Message.JobId);

                e.UsePartitioner<IJobSlotWaitElapsed>(partition, p => p.Message.JobId);
                e.UsePartitioner<IJobRetryDelayElapsed>(partition, p => p.Message.JobId);
            }

            var stateMachine = new JobStateMachine();
            e.StateMachineSaga(stateMachine, _jobRepository ?? new InMemorySagaRepository<JobSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobSaga>, IJobSagaSettings>(_options)));

            _jobSagaEndpointConfigurator = e;

            _options.JobSagaEndpointAddress = e.InputAddress;
        });

        _busConfigurator.ReceiveEndpoint(_options.JobAttemptEndpointName, e =>
        {
            e.UseTechnicalMessageRetry();

            UseVolatileOutbox(e);

            if (_options.ConcurrentMessageLimit.HasValue)
            {
                e.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

                var partition = new PartitionCoordinator(_options.ConcurrentMessageLimit.Value);

                e.UsePartitioner<IStartJobAttempt>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<IFinalizeJobAttempt>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<ICancelJobAttempt>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<Fault<IStartJob>>(partition, p => p.Message.Message.AttemptId);

                e.UsePartitioner<IJobAttemptStarted>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<IJobAttemptCompleted>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<IJobAttemptCanceled>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<IJobAttemptFaulted>(partition, p => p.Message.AttemptId);

                e.UsePartitioner<IJobAttemptStatus>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<IJobStatusCheckRequested>(partition, p => p.Message.AttemptId);
            }

            var stateMachine = new JobAttemptStateMachine();
            e.StateMachineSaga(stateMachine, _jobAttemptRepository ?? new InMemorySagaRepository<JobAttemptSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobAttemptSaga>, IJobSagaSettings>(_options)));

            _jobAttemptSagaEndpointConfigurator = e;

            _options.JobAttemptSagaEndpointAddress = e.InputAddress;
        });

        _busConfigurator.ReceiveEndpoint(_options.JobTypeEndpointName, e =>
        {
            e.UseTechnicalMessageRetry();

            UseVolatileOutbox(e);

            if (_options.ConcurrentMessageLimit.HasValue)
            {
                e.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

                var partition = new PartitionCoordinator(_options.ConcurrentMessageLimit.Value);

                e.UsePartitioner<IAllocateJobSlot>(partition, p => p.Message.JobTypeId);
                e.UsePartitioner<IJobSlotReleased>(partition, p => p.Message.JobTypeId);
                e.UsePartitioner<ISetConcurrentJobLimit>(partition, p => p.Message.JobTypeId);
            }

            var stateMachine = new JobTypeStateMachine();

            e.StateMachineSaga(stateMachine, _jobTypeRepository ?? new InMemorySagaRepository<JobTypeSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobTypeSaga>, IJobSagaSettings>(_options)));

            _jobTypeSagaEndpointConfigurator = e;

            _options.JobTypeSagaEndpointAddress = e.InputAddress;
        });

        _endpointsConfigured = true;
    }
}
