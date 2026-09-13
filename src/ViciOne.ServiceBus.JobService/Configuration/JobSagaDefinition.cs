using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Partitioning;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies endpoint, partitioning, outbox, retry, and settings behavior to job coordination.</summary>
internal sealed class JobSagaDefinition :
    SagaDefinition<JobSaga>
{
    readonly JobSagaOptions _options;
    readonly IJobSagaSettingsConfigurator _setOptions;

    /// <summary>Captures the shared job-saga settings used to configure job coordination.</summary>
    /// <param name="options">The validated job-saga settings wrapper.</param>
    public JobSagaDefinition(IOptions<JobSagaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentException("The options wrapper returned no value.", nameof(options));
        _setOptions = _options;
    }

    /// <summary>Applies reliability, partitioning, shared settings, and startup dependencies to job lifecycle coordination.</summary>
    /// <param name="configurator">The receive endpoint that hosts job lifecycle coordination.</param>
    /// <param name="sagaConfigurator">The job-state persistence pipeline.</param>
    /// <param name="context">The bus registration context.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator configurator, ISagaConfigurator<JobSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        configurator.UseTechnicalMessageRetry();

        configurator.UseMessageScope(context);

        configurator.UseVolatileOutbox(context);

        if (_options.ConcurrentMessageLimit.HasValue)
        {
            configurator.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

            var partition = new PartitionCoordinator(_options.ConcurrentMessageLimit.Value);

            configurator.UsePartitioner<IJobSubmitted>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<IJobSlotAllocated>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IJobSlotUnavailable>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<Fault<IAllocateJobSlot>>(partition, p => p.Message.Message.JobId);

            configurator.UsePartitioner<Fault<IStartJobAttempt>>(partition, p => p.Message.Message.JobId);

            configurator.UsePartitioner<IJobAttemptCanceled>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IJobAttemptCompleted>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IJobAttemptFaulted>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IJobAttemptStarted>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<IGetJobState>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<IJobCompleted>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<ICancelJob>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IRetryJob>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IRunJob>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<ISetJobProgress>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<ISaveJobCheckpoint>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<IJobSlotWaitElapsed>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<IJobRetryDelayElapsed>(partition, p => p.Message.JobId);
        }

        sagaConfigurator.UseFilter(new PayloadFilter<SagaConsumeContext<JobSaga>, IJobSagaSettings>(_options));

        _setOptions.JobSagaEndpointAddress = configurator.InputAddress;

        if (context.GetRequiredService<IContainerSelector>().TryGetRegistration(context, typeof(JobServiceState), out IJobServiceRegistration? registration))
            registration.AddReceiveEndpointDependency(configurator);
    }
}
