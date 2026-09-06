using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Partitions recurring-schedule pause commands by their Quartz trigger identity.</summary>
public class PauseScheduledMessageConsumerDefinition :
    ConsumerDefinition<PauseScheduledMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public PauseScheduledMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
    {
        _endpointDefinition = endpointDefinition;

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Serializes pause commands that address the same recurring trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The pause consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PauseScheduledMessageConsumer> consumerConfigurator, IRegistrationContext context)
    {
        consumerConfigurator.Message<PauseScheduledRecurringMessage>(m =>
        {
            m.UsePartitioner(_endpointDefinition.Partition, p => $"{p.Message.ScheduleGroup},{p.Message.ScheduleId}");
        });
    }
}
