using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Consumers;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz.Configuration;

/// <summary>Configures technical retry and trigger-identity partitioning for schedule commands.</summary>
internal sealed class ScheduleMessageConsumerDefinition<TBus> :
    ConsumerDefinition<ScheduleMessageConsumer<TBus>>
    where TBus : class, IBus
{
    readonly QuartzEndpointDefinition<TBus> _endpointDefinition;

    /// <summary>Initializes the consumer definition with the shared scheduling endpoint definition.</summary>
    /// <param name="endpointDefinition">The shared Quartz endpoint and partitioner definition.</param>
    public ScheduleMessageConsumerDefinition(QuartzEndpointDefinition<TBus> endpointDefinition)
    {
        _endpointDefinition = endpointDefinition ?? throw new ArgumentNullException(nameof(endpointDefinition));

        EndpointDefinition = endpointDefinition;
    }

    /// <summary>Adds technical retry and serializes schedule commands that address the same trigger.</summary>
    /// <param name="endpointConfigurator">The Quartz receive endpoint.</param>
    /// <param name="consumerConfigurator">The schedule consumer configuration.</param>
    /// <param name="context">The active registration context.</param>
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ScheduleMessageConsumer<TBus>> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseTechnicalMessageRetry();

        consumerConfigurator.Message<ScheduleMessage>(message =>
            message.UsePartitioner(_endpointDefinition.Partitioner, context => context.Message.TokenId));

        consumerConfigurator.Message<ScheduleRecurringMessage>(message =>
            message.UsePartitioner(_endpointDefinition.Partitioner, context => Runtime.QuartzTriggerKey.GetPartitionKey(
                context.Message.Schedule.ScheduleId,
                context.Message.Schedule.ScheduleGroup)));
    }
}
