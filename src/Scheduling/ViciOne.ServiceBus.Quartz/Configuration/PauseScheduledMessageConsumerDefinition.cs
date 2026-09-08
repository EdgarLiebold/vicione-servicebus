using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz.Configuration;

/// <summary>Partitions recurring-schedule pause commands by their Quartz trigger identity.</summary>
internal sealed class PauseScheduledMessageConsumerDefinition<TBus> :
    ConsumerDefinition<PauseScheduledMessageConsumer<TBus>>
    where TBus : class, IBus
{
    readonly QuartzEndpointDefinition<TBus> _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public PauseScheduledMessageConsumerDefinition(QuartzEndpointDefinition<TBus> endpointDefinition)
    {
        _endpointDefinition = endpointDefinition ?? throw new ArgumentNullException(nameof(endpointDefinition));

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Serializes pause commands that address the same recurring trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The pause consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PauseScheduledMessageConsumer<TBus>> consumerConfigurator, IRegistrationContext context)
    {
        consumerConfigurator.Message<PauseScheduledRecurringMessage>(message =>
            message.UsePartitioner(_endpointDefinition.Partition, context => Runtime.QuartzTriggerKey.GetPartitionKey(
                context.Message.ScheduleId,
                context.Message.ScheduleGroup)));
    }
}
