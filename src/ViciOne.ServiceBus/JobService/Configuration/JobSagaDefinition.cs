using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job saga definition implementation.
/// </summary>
public class JobSagaDefinition :
    SagaDefinition<JobSaga>
{
    readonly JobSagaOptions _options;
    readonly JobSagaSettingsConfigurator _setOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public JobSagaDefinition(IOptions<JobSagaOptions> options)
    {
        _options = options.Value;
        _setOptions = _options;
    }

    /// <summary>
    /// Configures saga.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="sagaConfigurator">The saga configurator value.</param>
    /// <param name="context">The operation context.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator configurator, ISagaConfigurator<JobSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        configurator.UseTechnicalMessageRetry();

        configurator.UseMessageScope(context);

        configurator.UseVolatileOutbox(context);

        if (_options.ConcurrentMessageLimit.HasValue)
        {
            configurator.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

            var partition = new Partitioner(_options.ConcurrentMessageLimit.Value, new Murmur3UnsafeHashGenerator());

            configurator.UsePartitioner<JobSubmitted>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<JobSlotAllocated>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<JobSlotUnavailable>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<Fault<AllocateJobSlot>>(partition, p => p.Message.Message.JobId);

            configurator.UsePartitioner<Fault<StartJobAttempt>>(partition, p => p.Message.Message.JobId);

            configurator.UsePartitioner<JobAttemptCanceled>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<JobAttemptCompleted>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<JobAttemptFaulted>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<JobAttemptStarted>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<GetJobState>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<JobCompleted>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<CancelJob>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<RetryJob>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<RunJob>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<SetJobProgress>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<SaveJobState>(partition, p => p.Message.JobId);

            configurator.UsePartitioner<JobSlotWaitElapsed>(partition, p => p.Message.JobId);
            configurator.UsePartitioner<JobRetryDelayElapsed>(partition, p => p.Message.JobId);
        }

        sagaConfigurator.UseFilter(new PayloadFilter<SagaConsumeContext<JobSaga>, JobSagaSettings>(_options));

        _setOptions.JobSagaEndpointAddress = configurator.InputAddress;

        if (context.GetRequiredService<IContainerSelector>().TryGetRegistration(context, typeof(JobServiceState), out IJobServiceRegistration? registration))
            registration.AddReceiveEndpointDependency(configurator);
    }
}
