using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures job-service coordination endpoints for a service instance.</summary>
/// <typeparam name="TReceiveEndpointConfigurator">The receive endpoint configurator type.</typeparam>
public sealed class JobServiceConfigurator<TReceiveEndpointConfigurator> :
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="instanceConfigurator">The instance configurator.</param>
    /// <param name="options">The options that control the operation.</param>
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

    /// <summary>Gets or sets the slot wait time.</summary>
    public TimeSpan SlotWaitTime
    {
        set => _options.SlotWaitTime = value;
    }

    /// <summary>Gets or sets the status check interval.</summary>
    public TimeSpan StatusCheckInterval
    {
        set => _options.StatusCheckInterval = value;
    }

    /// <summary>Gets or sets the suspect job retry count.</summary>
    public int SuspectJobRetryCount
    {
        set => _options.SuspectJobRetryCount = value;
    }

    /// <summary>Gets or sets the suspect job retry delay.</summary>
    public TimeSpan? SuspectJobRetryDelay
    {
        set => _options.SuspectJobRetryDelay = value;
    }

    /// <inheritdoc />
    public int? ConcurrentMessageLimit
    {
        set => _options.ConcurrentMessageLimit = value;
    }

    /// <summary>Gets or sets the finalize completed.</summary>
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

    /// <summary>Configures job service endpoints.</summary>
    /// <param name="context">The context associated with the operation.</param>
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

                var partition = new Partitioner(_options.ConcurrentMessageLimit.Value, new Murmur3UnsafeHashGenerator());

                e.UsePartitioner<JobSubmitted>(partition, p => p.Message.JobId);

                e.UsePartitioner<JobSlotAllocated>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobSlotUnavailable>(partition, p => p.Message.JobId);
                e.UsePartitioner<Fault<AllocateJobSlot>>(partition, p => p.Message.Message.JobId);

                e.UsePartitioner<Fault<StartJobAttempt>>(partition, p => p.Message.Message.JobId);

                e.UsePartitioner<JobAttemptCanceled>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobAttemptCompleted>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobAttemptFaulted>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobAttemptStarted>(partition, p => p.Message.JobId);

                e.UsePartitioner<GetJobState>(partition, p => p.Message.JobId);

                e.UsePartitioner<JobCompleted>(partition, p => p.Message.JobId);
                e.UsePartitioner<CancelJob>(partition, p => p.Message.JobId);
                e.UsePartitioner<RetryJob>(partition, p => p.Message.JobId);
                e.UsePartitioner<RunJob>(partition, p => p.Message.JobId);

                e.UsePartitioner<SaveJobCheckpoint>(partition, p => p.Message.JobId);
                e.UsePartitioner<SetJobProgress>(partition, p => p.Message.JobId);

                e.UsePartitioner<JobSlotWaitElapsed>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobRetryDelayElapsed>(partition, p => p.Message.JobId);
            }

            var stateMachine = new JobStateMachine();
            e.StateMachineSaga(stateMachine, _jobRepository ?? new InMemorySagaRepository<JobSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobSaga>, JobSagaSettings>(_options)));

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

                var partition = new Partitioner(_options.ConcurrentMessageLimit.Value, new Murmur3UnsafeHashGenerator());

                e.UsePartitioner<StartJobAttempt>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<FinalizeJobAttempt>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<CancelJobAttempt>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<Fault<StartJob>>(partition, p => p.Message.Message.AttemptId);

                e.UsePartitioner<JobAttemptStarted>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<JobAttemptCompleted>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<JobAttemptCanceled>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<JobAttemptFaulted>(partition, p => p.Message.AttemptId);

                e.UsePartitioner<JobAttemptStatus>(partition, p => p.Message.AttemptId);
                e.UsePartitioner<JobStatusCheckRequested>(partition, p => p.Message.AttemptId);
            }

            var stateMachine = new JobAttemptStateMachine();
            e.StateMachineSaga(stateMachine, _jobAttemptRepository ?? new InMemorySagaRepository<JobAttemptSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobAttemptSaga>, JobSagaSettings>(_options)));

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

                var partition = new Partitioner(_options.ConcurrentMessageLimit.Value, new Murmur3UnsafeHashGenerator());

                e.UsePartitioner<AllocateJobSlot>(partition, p => p.Message.JobTypeId);
                e.UsePartitioner<JobSlotReleased>(partition, p => p.Message.JobTypeId);
                e.UsePartitioner<SetConcurrentJobLimit>(partition, p => p.Message.JobTypeId);
            }

            var stateMachine = new JobTypeStateMachine();

            e.StateMachineSaga(stateMachine, _jobTypeRepository ?? new InMemorySagaRepository<JobTypeSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobTypeSaga>, JobSagaSettings>(_options)));

            _jobTypeSagaEndpointConfigurator = e;

            _options.JobTypeSagaEndpointAddress = e.InputAddress;
        });

        _endpointsConfigured = true;
    }
}
