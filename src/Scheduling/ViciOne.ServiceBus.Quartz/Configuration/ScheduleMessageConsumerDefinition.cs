using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures technical retry and trigger-identity partitioning for schedule commands.</summary>
public class ScheduleMessageConsumerDefinition :
    ConsumerDefinition<ScheduleMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public ScheduleMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
    {
        _endpointDefinition = endpointDefinition;

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Adds technical retry and serializes schedule commands that address the same trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The schedule consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ScheduleMessageConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalMessageRetry();

        consumerConfigurator.Message<ScheduleMessage>(m => m.UsePartitioner(_endpointDefinition.Partition, p => p.Message.TokenId));

        consumerConfigurator.Message<ScheduleRecurringMessage>(m =>
        {
            m.UsePartitioner(_endpointDefinition.Partition, p => $"{p.Message.Schedule?.ScheduleGroup},{p.Message.Schedule?.ScheduleId}");
        });
    }
}
