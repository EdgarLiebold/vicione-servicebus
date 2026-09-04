using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job service configurator implementation.
/// </summary>
/// <typeparam name="TReceiveEndpointConfigurator">The t receive endpoint configurator type.</typeparam>
public class JobServiceConfigurator<TReceiveEndpointConfigurator> :
    IJobServiceConfigurator,
    ISpecification
    where TReceiveEndpointConfigurator : IReceiveEndpointConfigurator
{
    readonly IReceiveConfigurator<TReceiveEndpointConfigurator> _busConfigurator;
    readonly JobServiceOptions _options;
    bool _endpointsConfigured;
    ISagaRepository<JobAttemptSaga> _jobAttemptRepository = null!;
    IReceiveEndpointConfigurator _jobAttemptSagaEndpointConfigurator = null!;
    ISagaRepository<JobSaga> _jobRepository = null!;
    IReceiveEndpointConfigurator _jobSagaEndpointConfigurator = null!;
    ISagaRepository<JobTypeSaga> _jobTypeRepository = null!;
    IReceiveEndpointConfigurator _jobTypeSagaEndpointConfigurator = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="instanceConfigurator">The instance configurator value.</param>
    /// <param name="options">The options value.</param>
    public JobServiceConfigurator(IServiceInstanceConfigurator<TReceiveEndpointConfigurator> instanceConfigurator, JobServiceOptions? options = null)
    {
        _busConfigurator = instanceConfigurator.BusConfigurator;

        _options = options != null
            ? instanceConfigurator.Options(options)
            : instanceConfigurator.Options<JobServiceOptions>();

        var settings = new InstanceJobServiceSettings(new JobConsumerOptions { HeartbeatInterval = _options.HeartbeatInterval })
        {
            InstanceEndpointConfigurator = instanceConfigurator.InstanceEndpointConfigurator,
            InstanceAddress = instanceConfigurator.InstanceAddress
        };

        settings.JobService.ConfigureSuperviseJobConsumer(instanceConfigurator.InstanceEndpointConfigurator);

        if (instanceConfigurator.BusConfigurator is IBusObserverConnector connector)
            connector.ConnectBusObserver(new JobServiceBusObserver(settings.JobService));

        instanceConfigurator.AddSpecification(this);

        _options.JobService = settings.JobService;
        _options.InstanceEndpointConfigurator = instanceConfigurator.InstanceEndpointConfigurator;

        _options.JobTypeSagaEndpointName = instanceConfigurator.EndpointNameFormatter.Saga<JobTypeSaga>();
        _options.JobStateSagaEndpointName = instanceConfigurator.EndpointNameFormatter.Saga<JobSaga>();
        _options.JobAttemptSagaEndpointName = instanceConfigurator.EndpointNameFormatter.Saga<JobAttemptSaga>();

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

    /// <summary>
    /// Gets or sets the repository value.
    /// </summary>
    public ISagaRepository<JobTypeSaga> Repository
    {
        set => _jobTypeRepository = value;
    }

    /// <summary>
    /// Gets or sets the job repository value.
    /// </summary>
    public ISagaRepository<JobSaga> JobRepository
    {
        set => _jobRepository = value;
    }

    /// <summary>
    /// Gets or sets the job attempt repository value.
    /// </summary>
    public ISagaRepository<JobAttemptSaga> JobAttemptRepository
    {
        set => _jobAttemptRepository = value;
    }

    /// <summary>
    /// Gets or sets the job service state endpoint name value.
    /// </summary>
    public string JobServiceStateEndpointName
    {
        set => _options.JobTypeSagaEndpointName = value;
    }

    /// <summary>
    /// Gets or sets the job service job state endpoint name value.
    /// </summary>
    public string JobServiceJobStateEndpointName
    {
        set => _options.JobStateSagaEndpointName = value;
    }

    /// <summary>
    /// Gets or sets the job service job attempt state endpoint name value.
    /// </summary>
    public string JobServiceJobAttemptStateEndpointName
    {
        set => _options.JobAttemptSagaEndpointName = value;
    }

    /// <summary>
    /// Gets or sets the slot wait time value.
    /// </summary>
    public TimeSpan SlotWaitTime
    {
        set => _options.SlotWaitTime = value;
    }

    /// <summary>
    /// Gets or sets the status check interval value.
    /// </summary>
    public TimeSpan StatusCheckInterval
    {
        set => _options.StatusCheckInterval = value;
    }

    /// <summary>
    /// Gets or sets the suspect job retry count value.
    /// </summary>
    public int SuspectJobRetryCount
    {
        set => _options.SuspectJobRetryCount = value;
    }

    /// <summary>
    /// Gets or sets the suspect job retry delay value.
    /// </summary>
    public TimeSpan SuspectJobRetryDelay
    {
        set
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "The delay must be > TimeSpan.Zero");

            _options.SuspectJobRetryDelay = value;
        }
    }

    /// <summary>
    /// Gets or sets the saga partition count value.
    /// </summary>
    public int? SagaPartitionCount
    {
        set => _options.SagaPartitionCount = value;
    }

    /// <summary>
    /// Gets or sets the finalize completed value.
    /// </summary>
    public bool FinalizeCompleted
    {
        set => _options.FinalizeCompleted = value;
    }

    /// <summary>
    /// Gets or sets the time zone resolver value.
    /// </summary>
    public Func<string, TimeZoneInfo> TimeZoneResolver
    {
        set => _options.TimeZoneResolver = value;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        ISpecification options = _options;

        return options.Validate();
    }

    /// <summary>
    /// Performs the on configure endpoint operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void OnConfigureEndpoint(Action<IReceiveEndpointConfigurator> callback)
    {
        _options.OnConfigureEndpoint = callback;
    }

    /// <summary>
    /// Configures job service endpoints.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void ConfigureJobServiceEndpoints(IRegistrationContext? context = null)
    {
        if (_endpointsConfigured)
            return;

        void UseInMemoryOutbox(IReceiveEndpointConfigurator configurator)
        {
            if (context == null)
                configurator.UseInMemoryOutbox();
            else
            {
                configurator.UseMessageScope(context);
                configurator.UseInMemoryOutbox(context);
            }
        }

        _busConfigurator.ReceiveEndpoint(_options.JobStateSagaEndpointName, e =>
        {
            e.UseTechnicalMessageRetry();

            UseInMemoryOutbox(e);

            if (_options.SagaPartitionCount.HasValue)
            {
                e.ConcurrentMessageLimit = _options.SagaPartitionCount;

                var partition = new Partitioner(_options.SagaPartitionCount.Value, new Murmur3UnsafeHashGenerator());

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

                e.UsePartitioner<SaveJobState>(partition, p => p.Message.JobId);
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

        _busConfigurator.ReceiveEndpoint(_options.JobAttemptSagaEndpointName, e =>
        {
            e.UseTechnicalMessageRetry();

            UseInMemoryOutbox(e);

            if (_options.SagaPartitionCount.HasValue)
            {
                e.ConcurrentMessageLimit = _options.SagaPartitionCount;

                var partition = new Partitioner(_options.SagaPartitionCount.Value, new Murmur3UnsafeHashGenerator());

                e.UsePartitioner<StartJobAttempt>(partition, p => p.Message.JobId);
                e.UsePartitioner<FinalizeJobAttempt>(partition, p => p.Message.JobId);
                e.UsePartitioner<CancelJobAttempt>(partition, p => p.Message.JobId);
                e.UsePartitioner<Fault<StartJob>>(partition, p => p.Message.Message.JobId);

                e.UsePartitioner<JobAttemptStarted>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobAttemptCompleted>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobAttemptCanceled>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobAttemptFaulted>(partition, p => p.Message.JobId);

                e.UsePartitioner<JobAttemptStatus>(partition, p => p.Message.JobId);
                e.UsePartitioner<JobStatusCheckRequested>(partition, p => p.Message.JobId ?? p.Message.AttemptId);
            }

            var stateMachine = new JobAttemptStateMachine();
            e.StateMachineSaga(stateMachine, _jobAttemptRepository ?? new InMemorySagaRepository<JobAttemptSaga>(),
                s => s.UseFilter(new PayloadFilter<SagaConsumeContext<JobAttemptSaga>, JobSagaSettings>(_options)));

            _jobAttemptSagaEndpointConfigurator = e;

            _options.JobAttemptSagaEndpointAddress = e.InputAddress;
        });

        _busConfigurator.ReceiveEndpoint(_options.JobTypeSagaEndpointName, e =>
        {
            e.UseTechnicalMessageRetry();

            UseInMemoryOutbox(e);

            if (_options.SagaPartitionCount.HasValue)
            {
                e.ConcurrentMessageLimit = _options.SagaPartitionCount;

                var partition = new Partitioner(_options.SagaPartitionCount.Value, new Murmur3UnsafeHashGenerator());

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
