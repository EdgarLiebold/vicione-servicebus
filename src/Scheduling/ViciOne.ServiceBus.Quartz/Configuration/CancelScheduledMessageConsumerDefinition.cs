using ViciOne.ServiceBus.Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Partitions one-time and recurring cancellation commands by their Quartz trigger identity.</summary>
public class CancelScheduledMessageConsumerDefinition :
    ConsumerDefinition<CancelScheduledMessageConsumer>
{
    readonly QuartzEndpointDefinition _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public CancelScheduledMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
    {
        _endpointDefinition = endpointDefinition;

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Serializes cancellation commands that address the same trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The cancellation consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelScheduledMessageConsumer> consumerConfigurator, IRegistrationContext context)
    {
        consumerConfigurator.Message<CancelScheduledMessage>(m => m.UsePartitioner(_endpointDefinition.Partition, p => p.Message.TokenId));

        consumerConfigurator.Message<CancelScheduledRecurringMessage>(m =>
        {
            m.UsePartitioner(_endpointDefinition.Partition, p => $"{p.Message.ScheduleGroup},{p.Message.ScheduleId}");
        });
    }
}
