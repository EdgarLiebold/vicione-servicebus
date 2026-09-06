using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies endpoint, partitioning, outbox, retry, and settings behavior to distributed capacity coordination.</summary>
internal sealed class JobTypeSagaDefinition :
    SagaDefinition<JobTypeSaga>
{
    readonly JobSagaOptions _options;
    readonly JobSagaSettingsConfigurator _setOptions;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public JobTypeSagaDefinition(IOptions<JobSagaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentException("The options wrapper returned no value.", nameof(options));
        _setOptions = _options;
    }

    /// <summary>Applies reliability, partitioning, shared settings, and startup dependencies to capacity coordination.</summary>
    /// <param name="configurator">The receive endpoint that hosts job-type capacity coordination.</param>
    /// <param name="sagaConfigurator">The job-type persistence pipeline.</param>
    /// <param name="context">The bus registration context.</param>
    protected override void ConfigureSaga(IReceiveEndpointConfigurator configurator, ISagaConfigurator<JobTypeSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        configurator.UseTechnicalMessageRetry();

        configurator.UseMessageScope(context);

        configurator.UseVolatileOutbox(context);

        if (_options.ConcurrentMessageLimit.HasValue)
        {
            configurator.ConcurrentMessageLimit = _options.ConcurrentMessageLimit;

            var partition = new Partitioner(_options.ConcurrentMessageLimit.Value, new Murmur3UnsafeHashGenerator());

            configurator.UsePartitioner<AllocateJobSlot>(partition, p => p.Message.JobTypeId);
            configurator.UsePartitioner<JobSlotReleased>(partition, p => p.Message.JobTypeId);
            configurator.UsePartitioner<SetConcurrentJobLimit>(partition, p => p.Message.JobTypeId);
        }

        sagaConfigurator.UseFilter(new PayloadFilter<SagaConsumeContext<JobTypeSaga>, JobSagaSettings>(_options));

        _setOptions.JobTypeSagaEndpointAddress = configurator.InputAddress;

        if (context.GetRequiredService<IContainerSelector>().TryGetRegistration(context, typeof(JobServiceState), out IJobServiceRegistration? registration))
            registration.AddReceiveEndpointDependency(configurator);
    }
}
