using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using JobServiceState = ViciOne.ServiceBus.JobService.JobService;

namespace ViciOne.ServiceBus.Configuration;

public class JobTypeSagaDefinition :
    SagaDefinition<JobTypeSaga>
{
    readonly JobSagaOptions _options;
    readonly JobSagaSettingsConfigurator _setOptions;

    public JobTypeSagaDefinition(IOptions<JobSagaOptions> options)
    {
        _options = options.Value;
        _setOptions = _options;
    }

    protected override void ConfigureSaga(IReceiveEndpointConfigurator configurator, ISagaConfigurator<JobTypeSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        configurator.UseTechnicalMessageRetry();

        configurator.UseMessageScope(context);

        configurator.UseInMemoryOutbox(context);

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
