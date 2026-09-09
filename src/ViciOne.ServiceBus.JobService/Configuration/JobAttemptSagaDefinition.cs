using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Partitioning;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies endpoint, partitioning, outbox, retry, and settings behavior to attempt supervision.</summary>
internal sealed class JobAttemptSagaDefinition :
    SagaDefinition<JobAttemptSaga>
{
    readonly JobSagaOptions _options;
    readonly JobSagaSettingsConfigurator _setOptions;

    /// <summary>Captures the shared job-saga settings used to configure attempt supervision.</summary>
    /// <param name="options">The validated job-saga settings wrapper.</param>
    public JobAttemptSagaDefinition(IOptions<JobSagaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentException("The options wrapper returned no value.", nameof(options));
        _setOptions = _options;
    }

    /// <summary>Applies reliability, partitioning, shared settings, and startup dependencies to attempt supervision.</summary>
    /// <param name="configurator">The receive endpoint that hosts attempt supervision.</param>
    /// <param name="sagaConfigurator">The attempt-state persistence pipeline.</param>
    /// <param name="context">The bus registration context.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator configurator, ISagaConfigurator<JobAttemptSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        configurator.UseTechnicalMessageRetry();

        configurator.UseMessageScope(context);

        configurator.UseVolatileOutbox(context);

        if (_options.ConcurrentMessageLimit.HasValue)
        {
            configurator.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

            var partition = new PartitionCoordinator(_options.ConcurrentMessageLimit.Value);

            configurator.UsePartitioner<StartJobAttempt>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<FinalizeJobAttempt>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<CancelJobAttempt>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<Fault<StartJob>>(partition, p => p.Message.Message.AttemptId);

            configurator.UsePartitioner<JobAttemptStarted>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<JobAttemptCompleted>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<JobAttemptCanceled>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<JobAttemptFaulted>(partition, p => p.Message.AttemptId);

            configurator.UsePartitioner<JobAttemptStatus>(partition, p => p.Message.AttemptId);
            configurator.UsePartitioner<JobStatusCheckRequested>(partition, p => p.Message.AttemptId);
        }

        sagaConfigurator.UseFilter(new PayloadFilter<SagaConsumeContext<JobAttemptSaga>, JobSagaSettings>(_options));

        _setOptions.JobAttemptSagaEndpointAddress = configurator.InputAddress;

        if (context.GetRequiredService<IContainerSelector>().TryGetRegistration(context, typeof(JobServiceState), out IJobServiceRegistration? registration))
            registration.AddReceiveEndpointDependency(configurator);
    }
}
