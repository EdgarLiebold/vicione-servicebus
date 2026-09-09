using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz.Configuration;

/// <summary>Partitions one-time and recurring cancellation commands by their Quartz trigger identity.</summary>
internal sealed class CancelScheduledMessageConsumerDefinition<TBus> :
    ConsumerDefinition<CancelScheduledMessageConsumer<TBus>>
    where TBus : class, IBus
{
    readonly QuartzEndpointDefinition<TBus> _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public CancelScheduledMessageConsumerDefinition(QuartzEndpointDefinition<TBus> endpointDefinition)
    {
        _endpointDefinition = endpointDefinition ?? throw new ArgumentNullException(nameof(endpointDefinition));

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Serializes cancellation commands that address the same trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The cancellation consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelScheduledMessageConsumer<TBus>> consumerConfigurator, IRegistrationContext context)
    {
        consumerConfigurator.Message<CancelScheduledMessage>(message =>
            message.UsePartitioner(_endpointDefinition.Partitioner, context => context.Message.TokenId));

        consumerConfigurator.Message<CancelScheduledRecurringMessage>(message =>
            message.UsePartitioner(_endpointDefinition.Partitioner, context => Runtime.QuartzTriggerKey.GetPartitionKey(
                context.Message.ScheduleId,
                context.Message.ScheduleGroup)));
    }
}
