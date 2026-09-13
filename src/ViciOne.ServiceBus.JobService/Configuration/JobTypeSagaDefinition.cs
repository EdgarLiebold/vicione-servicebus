using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Partitioning;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies endpoint, partitioning, outbox, retry, and settings behavior to distributed capacity coordination.</summary>
internal sealed class JobTypeSagaDefinition :
    SagaDefinition<JobTypeSaga>
{
    readonly JobSagaOptions _options;
    readonly IJobSagaSettingsConfigurator _setOptions;

    /// <summary>Captures the shared job-saga settings used to configure job-type capacity coordination.</summary>
    /// <param name="options">The validated job-saga settings wrapper.</param>
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

            var partition = new PartitionCoordinator(_options.ConcurrentMessageLimit.Value);

            configurator.UsePartitioner<IAllocateJobSlot>(partition, p => p.Message.JobTypeId);
            configurator.UsePartitioner<IJobSlotReleased>(partition, p => p.Message.JobTypeId);
            configurator.UsePartitioner<ISetConcurrentJobLimit>(partition, p => p.Message.JobTypeId);
        }

        sagaConfigurator.UseFilter(new PayloadFilter<SagaConsumeContext<JobTypeSaga>, IJobSagaSettings>(_options));

        _setOptions.JobTypeSagaEndpointAddress = configurator.InputAddress;

        if (context.GetRequiredService<IContainerSelector>().TryGetRegistration(context, typeof(JobServiceState), out IJobServiceRegistration? registration))
            registration.AddReceiveEndpointDependency(configurator);
    }
}
