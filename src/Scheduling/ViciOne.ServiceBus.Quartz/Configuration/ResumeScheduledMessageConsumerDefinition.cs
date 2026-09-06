using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Partitions recurring-schedule resume commands by their Quartz trigger identity.</summary>
public class ResumeScheduledMessageConsumerDefinition :
    ConsumerDefinition<ResumeScheduledMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public ResumeScheduledMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
    {
        _endpointDefinition = endpointDefinition;

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Serializes resume commands that address the same recurring trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The resume consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ResumeScheduledMessageConsumer> consumerConfigurator, IRegistrationContext context)
    {
        consumerConfigurator.Message<ResumeScheduledRecurringMessage>(m =>
        {
            m.UsePartitioner(_endpointDefinition.Partition, p => $"{p.Message.ScheduleGroup},{p.Message.ScheduleId}");
        });
    }
}
